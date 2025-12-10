
namespace FyreVMDemo.Game
{
    public enum eGlulxCommands
    {
        Look,
        Wait,
        NONE
    }

    public enum eStateOutputChannels
    {
        EndGame,
        Location,
        NONE
    }

    public class GameConstants
    {
        public static string OutputChannelToString(eStateOutputChannels stateOutputChannel)
        {
            switch (stateOutputChannel)
            {
                case eStateOutputChannels.EndGame:
                    return "ENDG";
                case eStateOutputChannels.Location:
                    return "LOCA";
                default:
                    return "NONE";
            }
        }

        public static eStateOutputChannels OutputChannelToEnum(string sOutputChannel)
        {
            switch (sOutputChannel.ToUpperInvariant())
            {
                case "ENDG":
                    return eStateOutputChannels.EndGame;
                case "LOCA":
                    return eStateOutputChannels.Location;
                default:
                    return eStateOutputChannels.NONE;
            }
        }
    }
}