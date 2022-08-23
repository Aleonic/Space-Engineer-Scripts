// Damage Monitor

public Program()
{
    // Determines repeat Frequency.
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

// The name of the LCD Panel is entered here.
private string displayName = "";


public void Main(string argument)
{
    if(displayName == "") return;

    // This will search for the LCD Panel on the connected grid.
    var display = (IMyTextPanel) GridTerminalSystem.GetBlockWithName(displayName);

    // Collects all items on grid.
    var list = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocks(list);

    // Handles text display.
    string displayText = displayString(list);
    if (displayText == "") {
        displayText = "All devices currently functional.";
    }
    display.WriteText(displayText, false);
}

public string displayString(List<IMyTerminalBlock> blocks) {
    string output = "";
    foreach (var block in blocks) {
        if(!block.IsFunctional) {
            output += block.DisplayNameText + " is damaged.\n";
        }
    }

    return output;
}
