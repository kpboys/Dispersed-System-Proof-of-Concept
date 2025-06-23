
public static class DevelopEnvLoader
{
    public static void Load(string filePath)
    {

        //string root = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
        string root = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.FullName;
        string actualPath = Path.Combine(root, filePath);
        if (!File.Exists(actualPath))
        {
            throw new FileNotFoundException("File was not found; Check if filepath was correct.");
        }

        string[] lines = File.ReadAllLines(actualPath);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            line = line.Replace(" ", "");
            var parts = line.Split(
                '=',
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                continue;

            string a = parts[0];
            string b = parts[1];
            Environment.SetEnvironmentVariable(a, b);
        }
    }
}
