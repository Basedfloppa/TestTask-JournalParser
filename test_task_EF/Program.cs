using System.Collections;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;

namespace test_task_EF
{
    internal class Program
    {
        internal static Dictionary<string,Argument> arguments = new Dictionary<string,Argument>()
        {
            ["file-log"] = new Argument("", true, "path"),
            ["file-output"] = new Argument("", true, "path"),
            ["address-start"] = new Argument("", false, "ip"),
            ["address-mask"] = new Argument("", false, "ip_mask"),
            ["time-start"] = new Argument("", true, "date"),
            ["time-end"] = new Argument("", true, "date"),
        };
        static void Main()
        {
            try 
            {
                ConsoleHelper.Info("Starting application");

                arguments = Parser.ParseArguments(arguments);
                ConsoleHelper.Info("Parsed arguments successfully");

                List<JournalEntry> entries;
                if (File.Exists(arguments["file-log"].Value)) 
                {
                    List<string> file = File.ReadAllLines(arguments["file-log"].Value).ToList();
                    entries = Parser.ParseJournal(file);
                    ConsoleHelper.Info("Opened journal successfully");
                }
                else
                {
                    throw new Exception("File with set path does not exists");
                }
                

                if (!String.IsNullOrEmpty(arguments["address-start"].Value))
                {
                    entries = Filter.FilterByIpRange(arguments["addtess-start"].Value, entries);
                    ConsoleHelper.Info("Filtered entries by address start successfully");

                    if(!String.IsNullOrEmpty(arguments["adress-mask"].Value))
                    {
                        entries = Filter.FilterByIpMask(arguments["address-mask"].Value, entries);
                        ConsoleHelper.Info("Filtered entries by address mask successfully");
                    }
                }

                entries = Filter.FilterByDate(arguments["time-start"].Value, arguments["time-end"].Value, entries);
                ConsoleHelper.Info("Filtered entries by date range successfully");

                Dictionary<string, int> entryCounts = new Dictionary<string, int>();
                foreach (JournalEntry entry in entries)
                {
                    if (entryCounts.ContainsKey(entry.Address)) entryCounts[entry.Address]++;
                    else entryCounts[entry.Address] = 1;
                }

                string file_path = arguments["file-output"].Value + "result.txt";
                foreach (string key in entryCounts.Keys)
                {
                    File.AppendAllText(file_path, $"{key} -> {entryCounts[key]}");
                }
                ConsoleHelper.Success("File successfully saved");
            }
            catch (Exception ex)
            {
                ConsoleHelper.Error(ex.Message);
            }
        }
    }
    public static class Filter
    {
        public static List<JournalEntry> FilterByDate(string _start, string _end, List<JournalEntry> entries)
        {
            DateTime start = Convert.ToDateTime(_start);
            DateTime end = Convert.ToDateTime(_end);
            return entries.Where(e => e.DateTime > start && e.DateTime < end).ToList();
        }
        public static List<JournalEntry> FilterByIpRange(string lower_range, List<JournalEntry> entries)
        {
            IPAddress? baseIP;
            IPAddress.TryParse(lower_range, out baseIP);
            byte[] byte_ip = baseIP.GetAddressBytes();
            List<JournalEntry> result = new List<JournalEntry>();

            foreach (JournalEntry entry in entries)
            {
                int counter = 0;
                byte[] byte_entry = IPAddress.Parse(entry.Address).GetAddressBytes();
                for (int i = 0; i <= 3; i++)
                {
                    if (byte_ip[i] <= byte_entry[i]) counter++;
                }
                if (counter == 4) result.Add(entry);
            }
            return result;
        }
        public static List<JournalEntry> FilterByIpMask(string mask, List<JournalEntry> entries)
        {
            if (Convert.ToInt32(mask) > 32 || Convert.ToInt32(mask) < 0) throw new ArgumentException("Provided mask is invalid");

            List<JournalEntry> result = new List<JournalEntry> ();
            foreach (JournalEntry entry in entries)
            {
                IPAddress ip = IPAddress.Parse(entry.Address);
                byte[] ip_bytes = ip.GetAddressBytes();

                uint mask_value = 0xFFFFFFFFU << (32 - Convert.ToInt32(mask));
                byte[] mask_bytes = BitConverter.GetBytes(mask_value);
                IPAddress maskAddress = new IPAddress(mask_bytes);

                byte[] network_bytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    network_bytes[i] = (byte)(ip_bytes[i] & mask_bytes[i]);
                }

                IPAddress network_ip = new IPAddress(network_bytes);
                if (ip.Equals(network_ip)) result.Add(entry);
            }
            return result;
        }
    }
    public static class Parser
    {
        public static Dictionary<string, Argument> ParseArguments(Dictionary<string, Argument> arguments)
        {
            foreach (var keyValuePair in arguments)
            {
                string key = keyValuePair.Key;
                Argument arg = keyValuePair.Value;
                arg.Value = GetArgument(key) ?? "";
                if (arg.Requiered)
                {
                    if (String.IsNullOrEmpty(arg.Value))
                    {
                        throw new ArgumentException($"Argument {key} was not supplied");
                    }
                    if (!Validate(arg.Value, arg.Type))
                    {
                        throw new ArgumentException($"Argument '{key}' was in the wrong format");
                    }
                }
            }
            return arguments;
        }
        public static List<JournalEntry> ParseJournal(List<string> File)
        {
            Regex address_pattern = new Regex(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$");
            Regex date_time_pattern = new Regex(@"[0-9]{4}-(0[1-9]|1[0-2])-(0[1-9]|[1-2][0-9]|3[0-1]) (2[0-3]|[01][0-9]):[0-5][0-9]:[0-5][0-9]");

            List<JournalEntry> result = new List<JournalEntry> {};

            foreach(string line in File)
            {
                int index = line.IndexOf(':');
                string address = line.Substring(0, index).Trim();
                string date_time = line.Substring(index + 1).Trim();

                if (address_pattern.IsMatch(address) && date_time_pattern.IsMatch(date_time))
                {
                    result.Add(new JournalEntry(address, DateTime.Parse(date_time)));
                }
            }

            return result;
        }
        internal static string? GetArgument(string name)
        {
            List<string> cmd_arguments = Environment.GetCommandLineArgs().ToList();
            if (cmd_arguments.Contains("--" + name) && (cmd_arguments.Count - 1) > cmd_arguments.IndexOf("--" + name)) 
            {
                return cmd_arguments[cmd_arguments.IndexOf("--" + name) + 1]; 
            }

            IDictionary env_arguments = Environment.GetEnvironmentVariables();
            if (env_arguments.Contains(name)) 
            { 
                return env_arguments[key: name]?.ToString(); 
            }

            return ConfigurationManager.AppSettings[name];
        }
        internal static bool Validate(string value, string type)
        {
            switch (type)
            {
                case "path":
                    return Path.IsPathRooted(value);
                case "ip":
                    return IPAddress.TryParse(value, out IPAddress? _);
                case "ip_mask":
                    return int.TryParse(value, out int mask) && mask >= 0 && mask <= 32;
                case "date":
                    return DateTime.TryParse(value, out _);
                default:
                    throw new ArgumentException($"{type} is not supported");
            }
        }
    }
    public class Argument
    {
        public string Value { get; set;}
        public bool Requiered { get; set; }
        public string Type { get; set; }
        public Argument(string value, bool required, string type)
        {
            Value = value;
            Requiered = required;
            Type = type;
        }
    }
    public class JournalEntry
    {
        public string Address {set; get;}
        public DateTime DateTime {set; get;}
        public JournalEntry(string address, DateTime datetime)
        {
            Address = address;
            DateTime = datetime;
        }
    }
    public static class ConsoleHelper
    {
        public static void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.Blue;  
            Console.Write(message);
            Console.ResetColor(); 
            Console.WriteLine("");
        }
        public static void Success(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;  
            Console.Write(message);
            Console.ResetColor(); 
            Console.WriteLine("");
        }
        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;  
            Console.Write(message);
            Console.ResetColor(); 
            Console.WriteLine("");
        }
    }
}