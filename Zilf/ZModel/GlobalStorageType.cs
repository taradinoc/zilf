
namespace Zilf.ZModel
{
    enum GlobalStorageType
    {
        /// <summary>
        /// The global can be stored in a Z-machine global or a table.
        /// </summary>
        Any,
        /// <summary>
        /// The global is (or must be) stored in a Z-machine global.
        /// </summary>
        Hard,
        /// <summary>
        /// The global is stored in a table.
        /// </summary>
        Soft,
    }
}