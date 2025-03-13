public class ChildAircraft
{
    public string Name { get; set; }
    public string Callsign { get; set; }
    public string Status { get; set; }

    public ChildAircraft(string name, string callsign, string status)
    {
        // Explicit null checks to ensure we don't pass null to non-nullable properties
        Name = name ?? throw new ArgumentNullException(nameof(name), "Child aircraft name cannot be null.");
        Callsign = callsign ?? throw new ArgumentNullException(nameof(callsign), "Child aircraft callsign cannot be null.");
        Status = status ?? throw new ArgumentNullException(nameof(status), "Child aircraft status cannot be null.");
    }
}

public class Aircraft
{
    public string Name { get; set; }
    public string Callsign { get; set; }
    public List<ChildAircraft> Children { get; set; }

    public Aircraft(string name, string callsign)
    {
        // Explicit null checks to ensure we don't pass null to non-nullable properties
        Name = name ?? throw new ArgumentNullException(nameof(name), "Aircraft name cannot be null.");
        Callsign = callsign ?? throw new ArgumentNullException(nameof(callsign), "Aircraft callsign cannot be null.");
        Children = new List<ChildAircraft>();
    }

    public void AddChild(ChildAircraft child)
    {
        Children.Add(child);
    }
}

public class Program
{
    public static void Main()
    {
        List<Aircraft> aircraftList = new List<Aircraft>();

        while (true)
        {
            Console.WriteLine("1. Add Aircraft");
            Console.WriteLine("2. Add Child to Aircraft");
            Console.WriteLine("3. Display All Aircraft");
            Console.WriteLine("4. Exit");
            Console.Write("Choose an option: ");
            string choice = Console.ReadLine() ?? string.Empty;

            if (choice == "1")
            {
                AddAircraft(aircraftList);
            }
            else if (choice == "2")
            {
                AddChildToAircraft(aircraftList);
            }
            else if (choice == "3")
            {
                DisplayAircraft(aircraftList);
            }
            else if (choice == "4")
            {
                break;
            }
            else
            {
                Console.WriteLine("Invalid choice. Try again.");
            }
        }
    }

    private static void AddAircraft(List<Aircraft> aircraftList)
    {
        string name = string.Empty;
        string callsign = string.Empty;

        do
        {
            Console.Write("Enter Aircraft Name: ");
            name = Console.ReadLine() ?? string.Empty;
        } while (string.IsNullOrEmpty(name));  // Ensure name is not empty or null

        do
        {
            Console.Write("Enter Aircraft Callsign: ");
            callsign = Console.ReadLine() ?? string.Empty;
        } while (string.IsNullOrEmpty(callsign));  // Ensure callsign is not empty or null

        // Create Aircraft and add to the list
        Aircraft newAircraft = new Aircraft(name, callsign);
        aircraftList.Add(newAircraft);

        Console.WriteLine($"Aircraft {name} ({callsign}) added.");
    }

    private static void AddChildToAircraft(List<Aircraft> aircraftList)
    {
        string parentName = string.Empty;
        string childName = string.Empty;
        string childCallsign = string.Empty;
        string childStatus = string.Empty;

        Console.Write("Enter Parent Aircraft Name: ");
        parentName = Console.ReadLine() ?? string.Empty;

        // Check if parentName is empty or null
        if (string.IsNullOrEmpty(parentName))
        {
            Console.WriteLine("Parent aircraft name cannot be empty.");
            return;
        }

        // Find the parent aircraft
        Aircraft? parentAircraft = aircraftList.Find(a => a.Name.Equals(parentName, StringComparison.OrdinalIgnoreCase));

        if (parentAircraft != null)
        {
            // Prompt for child details with null checks
            do
            {
                Console.Write("Enter Child Aircraft Name: ");
                childName = Console.ReadLine() ?? string.Empty;
            } while (string.IsNullOrEmpty(childName));

            do
            {
                Console.Write("Enter Child Aircraft Callsign: ");
                childCallsign = Console.ReadLine() ?? string.Empty;
            } while (string.IsNullOrEmpty(childCallsign));

            do
            {
                Console.Write("Enter Child Aircraft Status (Passed/Unpassed): ");
                childStatus = Console.ReadLine() ?? string.Empty;
            } while (string.IsNullOrEmpty(childStatus) || (childStatus != "Passed" && childStatus != "Unpassed"));

            // Creating child with validated values
            ChildAircraft newChild = new ChildAircraft(childName, childCallsign, childStatus);
            parentAircraft.AddChild(newChild);

            Console.WriteLine($"Child {childName} ({childCallsign}) with status {childStatus} added to {parentAircraft.Name}.");
        }
        else
        {
            Console.WriteLine("Parent aircraft not found.");
        }
    }

    private static void DisplayAircraft(List<Aircraft> aircraftList)
    {
        if (aircraftList.Count == 0)
        {
            Console.WriteLine("No aircraft in the database.");
            return;
        }

        // Display aircraft and their children
        foreach (var aircraft in aircraftList)
        {
            Console.WriteLine($"{aircraft.Name} ({aircraft.Callsign}) has the following children:");
            foreach (var child in aircraft.Children)
            {
                Console.WriteLine($"- {child.Name} ({child.Callsign}) - Status: {child.Status}");
            }
            Console.WriteLine();
        }
    }
}