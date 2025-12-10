/*
 * Copyright © 2008, Textfyre, Inc. - All Rights Reserved
 * Please read the accompanying COPYRIGHT file for licensing resstrictions.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FyreVM
{
    internal struct HeapEntry
    {
        public uint Start, Length;

        public HeapEntry(uint start, uint length)
        {
            this.Start = start;
            this.Length = length;
        }

        public override string ToString()
        {
            return string.Format("Start={0}, Length={1}", Start, Length);
        }
    }

    internal delegate bool MemoryRequester(uint newEndMem);

    /// <summary>
    /// Manages the heap size and block allocation for the malloc/mfree opcodes.
    /// </summary>
    /// <remarks>
    /// If Inform ever starts using the malloc opcode directly, instead of
    /// its own heap allocator, this should be made a little smarter.
    /// Currently we make no attempt to avoid heap fragmentation.
    /// </remarks>
    internal class HeapAllocator
    {
        private class EntryComparer : IComparer<HeapEntry>
        {
            public int Compare(HeapEntry x, HeapEntry y)
            {
                return x.Start.CompareTo(y.Start);
            }
        }

        private static readonly EntryComparer entryComparer = new EntryComparer();

        private readonly uint heapAddress;
        private readonly MemoryRequester setEndMem;
        private readonly List<HeapEntry> blocks;    // sorted
        private readonly List<HeapEntry> freeList;  // sorted

        private uint endMem;
        private uint heapExtent;
        private uint maxHeapExtent;

        /// <summary>
        /// Initializes a new allocator with an empty heap.
        /// </summary>
        /// <param name="heapAddress">The address where the heap will start.</param>
        /// <param name="requester">A delegate to request more memory.</param>
        public HeapAllocator(uint heapAddress, MemoryRequester requester)
        {
            this.heapAddress = heapAddress;
            this.setEndMem = requester;
            this.blocks = new List<HeapEntry>();
            this.freeList = new List<HeapEntry>();

            endMem = heapAddress;
            heapExtent = 0;
        }

        /// <summary>
        /// Initializes a new allocator from a previous saved heap state.
        /// </summary>
        /// <param name="savedHeap">A byte array describing the heap state,
        /// as returned by the <see cref="Save"/> method.</param>
        /// <param name="requester">A delegate to request more memory.</param>
        public HeapAllocator(byte[] savedHeap, MemoryRequester requester)
        {
            this.heapAddress = BigEndian.ReadInt32(savedHeap, 0);
            this.setEndMem = requester;
            this.blocks = new List<HeapEntry>();
            this.freeList = new List<HeapEntry>();

            uint numBlocks = BigEndian.ReadInt32(savedHeap, 4);
            blocks.Capacity = (int)numBlocks;
            uint nextAddress = heapAddress;

            for (uint i = 0; i < numBlocks; i++)
            {
                uint start = BigEndian.ReadInt32(savedHeap, 8 * i + 8);
                uint length = BigEndian.ReadInt32(savedHeap, 8 * i + 12);
                blocks.Add(new HeapEntry(start, length));

                if (nextAddress < start)
                    freeList.Add(new HeapEntry(nextAddress, start - nextAddress));

                nextAddress = start + length;
            }

            endMem = nextAddress;
            heapExtent = nextAddress - heapAddress;

            if (setEndMem(endMem) == false)
                throw new ArgumentException("Can't allocate VM memory to fit saved heap");

            blocks.Sort(entryComparer);
            freeList.Sort(entryComparer);
        }

        /// <summary>
        /// Gets the address where the heap starts.
        /// </summary>
        public uint Address
        {
            get { return heapAddress; }
        }

        /// <summary>
        /// Gets the size of the heap, in bytes.
        /// </summary>
        public uint Size
        {
            get { return heapExtent; }
        }

        /// <summary>
        /// Gets or sets the maximum allowed size of the heap, in bytes, or 0 to
        /// allow an unlimited heap.
        /// </summary>
        /// <remarks>
        /// When a maximum size is set, memory allocations will be refused if they
        /// would cause the heap to grow past the maximum size. Setting the maximum
        /// size to less than the current <see cref="Size"/> is allowed, but such a
        /// value will have no effect until deallocations cause the heap to shrink
        /// below the new maximum size.
        /// </remarks>
        public uint MaxSize
        {
            get { return maxHeapExtent; }
            set { maxHeapExtent = value; }
        }

        /// <summary>
        /// Gets the number of blocks that the allocator is managing.
        /// </summary>
        public int BlockCount
        {
            get { return blocks.Count; }
        }

        /// <summary>
        /// Saves the heap state to a byte array.
        /// </summary>
        /// <returns>A byte array describing the current heap state.</returns>
        public byte[] Save()
        {
            byte[] result = new byte[8 + blocks.Count * 8];

            BigEndian.WriteInt32(result, 0, heapAddress);
            BigEndian.WriteInt32(result, 4, (uint)blocks.Count);
            for (int i = 0; i < blocks.Count; i++)
            {
                BigEndian.WriteInt32(result, 8 * i + 8, blocks[i].Start);
                BigEndian.WriteInt32(result, 8 * i + 12, blocks[i].Length);
            }

            return result;
        }
        
        /// <summary>
        /// Allocates a new block on the heap.
        /// </summary>
        /// <param name="size">The size of the new block, in bytes.</param>
        /// <returns>The address of the new block, or 0 if allocation failed.</returns>
        public uint Alloc(uint size)
        {
            HeapEntry result = new HeapEntry(0, size);

            // look for a free block
            if (freeList != null)
            {
                for (int i = 0; i < freeList.Count; i++)
                {
                    HeapEntry entry = freeList[i];
                    if (entry.Length >= size)
                    {
                        result.Start = entry.Start;

                        if (entry.Length > size)
                        {
                            // shrink the free block
                            entry.Start += size;
                            entry.Length -= size;
                            freeList[i] = entry;
                        }
                        else
                            freeList.RemoveAt(i);

                        break;
                    }
                }
            }

            if (result.Start == 0)
            {
                // enforce maximum heap size
                if (maxHeapExtent != 0 && heapExtent + size > maxHeapExtent)
                    return 0;

                // add a new block at the end
                result = new HeapEntry(heapAddress + heapExtent, size);

                if (heapAddress + heapExtent + size > endMem)
                {
                    // grow the heap
                    uint newHeapAllocation = Math.Max(
                        heapExtent * 5 / 4,
                        heapExtent + size);

                    if (maxHeapExtent != 0)
                        newHeapAllocation = Math.Min(newHeapAllocation, maxHeapExtent);

                    if (setEndMem(heapAddress + newHeapAllocation))
                        endMem = heapAddress + newHeapAllocation;
                    else
                        return 0;
                }

                heapExtent += size;
            }

            // add the new block to the list
            int index = ~blocks.BinarySearch(result, entryComparer);
            System.Diagnostics.Debug.Assert(index >= 0);
            blocks.Insert(index, result);

            return result.Start;
        }

        /// <summary>
        /// Deallocates a previously allocated block.
        /// </summary>
        /// <param name="address">The address of the block to deallocate.</param>
        public void Free(uint address)
        {
            HeapEntry entry = new HeapEntry(address, 0);
            int index = blocks.BinarySearch(entry, entryComparer);

            if (index >= 0)
            {
                // delete the block
                entry = blocks[index];
                blocks.RemoveAt(index);

                // adjust the heap extent if necessary
                if (entry.Start + entry.Length - heapAddress == heapExtent)
                {
                    if (index == 0)
                    {
                        heapExtent = 0;
                    }
                    else
                    {
                        HeapEntry prev = blocks[index - 1];
                        heapExtent = prev.Start + prev.Length - heapAddress;
                    }
                }

                // add the block to the free list
                index = ~freeList.BinarySearch(entry, entryComparer);
                System.Diagnostics.Debug.Assert(index >= 0);
                freeList.Insert(index, entry);

                if (index < freeList.Count - 1)
                    Coalesce(index, index + 1);
                if (index > 0)
                    Coalesce(index - 1, index);

                // shrink the heap if necessary
                if (blocks.Count > 0 && heapExtent <= (endMem - heapAddress) / 2)
                {
                    if (setEndMem(heapAddress + heapExtent))
                    {
                        endMem = heapAddress + heapExtent;

                        for (int i = freeList.Count - 1; i >= 0; i--) {
                            if (freeList[i].Start >= endMem)
                                freeList.RemoveAt(i);
                        }
                    }
                }
            }
        }

        private void Coalesce(int index1, int index2)
        {
            HeapEntry first = freeList[index1];
            HeapEntry second = freeList[index2];

            if (first.Start + first.Length >= second.Start)
            {
                first.Length = second.Start + second.Length - first.Start;
                freeList[index1] = first;
                freeList.RemoveAt(index2);
            }
        }
    }
}
