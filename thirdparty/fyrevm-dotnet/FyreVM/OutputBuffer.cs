/*
 * Copyright © 2008, Textfyre, Inc. - All Rights Reserved
 * Please read the accompanying COPYRIGHT file for licensing resstrictions.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace FyreVM
{
    /// <summary>
    /// Collects output from the game file, on various output channels, to be
    /// delivered all at once.
    /// </summary>
    internal class OutputBuffer
    {
        private const uint DEFAULT_CHANNEL = ('M' << 24) | ('A' << 16) | ('I' << 8) | 'N';
        private uint channel = ('M' << 24) | ('A' << 16) | ('I' << 8) | 'N';
        private Dictionary<uint,StringBuilder> channelData;

        /// <summary>
        /// Initializes a new output buffer and adds the main channel.
        /// </summary>
        public OutputBuffer()
        {
            channelData = new Dictionary<uint, StringBuilder>();
            channelData.Add(DEFAULT_CHANNEL, new StringBuilder());
        }

        /// <summary>
        /// Gets or sets the current output channel.
        /// </summary>
        /// <remarks>
        /// If the output channel is changed to any channel other than
        /// <see cref="OutputChannel.Main"/>, the channel's contents will be
        /// cleared first.
        /// </remarks>
        public uint Channel
        {
            get { return channel; }
            set
            {
                if (channel != value)
                {
                    channel = value;
                    if (value != DEFAULT_CHANNEL)
                        if (!channelData.ContainsKey(channel))
                            channelData.Add(channel, new StringBuilder());
                        else
                            channelData[channel].Length = 0;
                }
            }
        }

        /// <summary>
        /// Writes a string to the buffer for the currently selected
        /// output channel.
        /// </summary>
        /// <param name="s">The string to write.</param>
        public void Write(string s)
        {
            if (!channelData.ContainsKey(channel))
                channelData.Add(channel, new StringBuilder(s));
            else
                channelData[channel].Append(s);
        }

        /// <summary>
        /// Writes a single character to the buffer for the currently selected
        /// output channel.
        /// </summary>
        /// <param name="c">The character to write.</param>
        public void Write(char c)
        {
            if (!channelData.ContainsKey(channel))
                channelData.Add(channel, new StringBuilder(c));
            else
                channelData[channel].Append(c);
        }


        /// <summary>
        /// Packages all the output that has been stored so far, returns it,
        /// and empties the buffer.
        /// </summary>
        /// <returns>A dictionary mapping each active output channel to the
        /// string of text that has been sent to it since the last flush.</returns>
        public IDictionary<string, string> Flush()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (KeyValuePair<uint, StringBuilder> pair in channelData)
            {
                string channelName = GetChannelName(pair.Key);

                if (pair.Value.Length > 0)
                {
                    result.Add(channelName, pair.Value.ToString());
                    pair.Value.Length = 0;
                }
            }

            return result;
        }

        private string GetChannelName(uint channelNumber)
        {
            return String.Concat((char)((channelNumber >> 24) & 0xff), (char)((channelNumber >> 16) & 0xff), (char)((channelNumber >> 8) & 0xff), (char)(channelNumber & 0xff));
        }
    }
}
