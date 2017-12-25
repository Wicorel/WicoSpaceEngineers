
IMyTextPanel textp;
IMyRadioAntenna ant;

public Program()
{
    textp = GridTerminalSystem.GetBlockWithName("receivelcd") as IMyTextPanel;
    textp?.WritePublicText("Receive Log:\n");
    List<IMyRadioAntenna> ants = new List<IMyRadioAntenna>();
    GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(ants);
    if (ants.Count > 0)
    {
        ant = ants[0];
        Echo("Found Antenna:" + ant.CustomName);
    }
    else Echo("No AntennaFound");
}

void Main(string argument, UpdateType ut)
{
    if ((ut & (UpdateType.Trigger | UpdateType.Terminal)) > 0)
    {
        if (argument == "clear")
            textp?.WritePublicText("Receive Log:\n");
        else
        {
            Echo("Transmit message");
            Echo(argument);
            if (ant != null) ant.TransmitMessage(argument);
        }
    }
    else if ((ut & (UpdateType.Antenna)) > 0)
    {
        textp?.WritePublicText(argument + "\n", true);
        Echo("Antenna Message Received:");
        Echo(argument);
    }
    else Echo("Unknown update Type");
}