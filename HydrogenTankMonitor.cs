//Hydrogen Tank Monitor

public Program()
{
    // Determines repeat Frequency.
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

// The name of the LCD Panel is entered here.
private string displayName = "Wide LCD panel Tank Monitor";


public void Main(string argument)
{
    if(displayName == "") return;

    // This will search for the LCD Panel on the connected grid.
    var display = (IMyTextPanel) GridTerminalSystem.GetBlockWithName(displayName);

    // Collects all items on grid.
    var list = new List<IMyGasTank>();
    GridTerminalSystem.GetBlocksOfType<IMyGasTank>(list);

    // Handles text display.
    string displayText = displayString(list);
    if (displayText == "") {
        displayText =  "All hydrogen tanks are full.";
    }
    display.WriteText(displayText, false);
}

public string displayString(List<IMyGasTank> blocks) {
    if (blocks == null || blocks.Count == 0) {
        return "";
    }

    double sum = 0;
    foreach (var block in blocks) {
            sum += block.FilledRatio;
    }

    string htTitle = "Hydrogen Tanks:\n";
    string htDetail_1 = "   Total tanks: " + blocks.Count + "\n";
    string htDetail_2 = "   Fill Average: " + ((sum/blocks.Count) * 100) + "\n";
    return htTitle + htDetail_1 + htDetail_2;
}
