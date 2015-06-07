using System.Diagnostics.Contracts;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    /// <summary>
    /// Provides methods common to structured types (LIST, STRING, possibly others).
    /// </summary>
    [ContractClass(typeof(IStructureContracts))]
    interface IStructure
    {
        /// <summary>
        /// Gets the first element of the structure.
        /// </summary>
        /// <returns>The first element.</returns>
        ZilObject GetFirst();
        /// <summary>
        /// Gets the remainder of the structure, after skipping the first few elements.
        /// </summary>
        /// <param name="skip">The number of elements to skip.</param>
        /// <returns>A structure containing the unskipped elements, or null if no elements are left.</returns>
        IStructure GetRest(int skip);

        /// <summary>
        /// Determines whether the structure is empty.
        /// </summary>
        /// <returns>true if the structure has no elements; false if it has any elements.</returns>
        [Pure]
        bool IsEmpty();

        /// <summary>
        /// Gets or sets an element by its numeric index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to access.</param>
        /// <returns>The element value, or null if the specified element is past the end of
        /// the structure.</returns>
        /// <exception cref="InterpreterError">
        /// An attempt was made to set an element past the end of the structure.
        /// </exception>
        ZilObject this[int index] { get; set; }

        /// <summary>
        /// Measures the length of the structure.
        /// </summary>
        /// <returns>The number of elements in the structure.</returns>
        /// <remarks>This method may loop indefinitely if the structure contains
        /// a reference to itself.</remarks>
        int GetLength();
        /// <summary>
        /// Measures the length of the structure, up to a specified maximum.
        /// </summary>
        /// <param name="limit">The maximum length to allow.</param>
        /// <returns>The number of elements in the structure, or null if the structure
        /// contains more than <paramref name="limit"/> elements.</returns>
        [Pure]
        int? GetLength(int limit);
    }
}