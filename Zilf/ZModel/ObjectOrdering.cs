
namespace Zilf.ZModel
{
    /// <summary>
    /// Specifies the order in which object numbers are assigned.
    /// </summary>
    enum ObjectOrdering
    {
        /// <summary>
        /// Reverse mention order.
        /// </summary>
        Default,
        /// <summary>
        /// Definition order, then mention order for objects with no definitions.
        /// </summary>
        Defined,
        /// <summary>
        /// Like <see cref="Defined"/>, but with all rooms having lower numbers than non-rooms.
        /// </summary>
        /// <remarks>
        /// "Rooms" include all objects declared with the ROOM FSUBR (instead of OBJECT), and
        /// all objects whose initial LOC is the object called ROOMS.
        /// </remarks>
        RoomsFirst,
        /// <summary>
        /// Like <see cref="RoomsFirst"/>, but with all local globals also having lower numbers
        /// than non-rooms and non-local-globals.
        /// </summary>
        /// <remarks>
        /// "Local globals" includes all objects whose initial LOC is the object called
        /// LOCAL-GLOBALS.
        /// </remarks>
        RoomsAndLocalGlobalsFirst,
        /// <summary>
        /// Like <see cref="Defined"/>, but with all non-rooms having lower numbers than rooms.
        /// </summary>
        RoomsLast,
    }
}