
namespace Zilf.ZModel
{
    /// <summary>
    /// Specifies the order of links in the object tree.
    /// </summary>
    /// <remarks>
    /// These values are named with regard to how the game will traverse the objects (following FIRST? and NEXT?).
    /// The compiler processes them in the opposite order when inserting them into the tree.
    /// </remarks>
    enum TreeOrdering
    {
        /// <summary>
        /// Reverse definition order, except for the first defined child of each parent, which remains the first child linked.
        /// </summary>
        Default,
        /// <summary>
        /// Reverse definition order.
        /// </summary>
        ReverseDefined,
    }
}