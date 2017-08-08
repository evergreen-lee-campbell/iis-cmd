using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using System.IO;

namespace IISLogParser
{
	class Program
	{
		static void Main(string[] args)
		{
			var options = new Options();

			long startTicks = DateTime.Now.Ticks;

			if (CommandLine.Parser.Default.ParseArguments(args, options))
			{
				if (String.IsNullOrEmpty(options.FilePath))
				{
					Console.WriteLine("You must speficy a log file path.");
					return;
				}

				try
				{
					Program.GetPrintFileSize(options.FilePath);
					bool doWriteFile = !String.IsNullOrEmpty(options.OutputFilePath);

					if (doWriteFile)
					{
						try
						{
							parseLog(options, File.OpenWrite(options.OutputFilePath));
						}
						catch (IOException ioe2)
						{
							Console.WriteLine("Couldn't write to new file: " + ioe2.Message);
							return;
						}
					}
					else
					{
						parseLog(options);
					}
				}
				catch (IOException ioe1)
				{
					Console.WriteLine("Unable to read file: " + ioe1.Message);
				}
			}

			long endTicks = DateTime.Now.Ticks;

			Console.WriteLine("\r\nFile parsing took " + TimeSpan.FromTicks(endTicks - startTicks).TotalMilliseconds + " ms.");

			Console.WriteLine("Press any key to exit.");
			Console.ReadKey();
		}

		static void parseLog(Options options)
		{
			string[] fileLines = File.ReadAllLines(options.FilePath);

			DateTime startDate;
			DateTime endDate;

			bool doStartDate = DateTime.TryParse(options.StartDateTime, out startDate);
			bool doEndDate = DateTime.TryParse(options.EndDateTime, out endDate);
			bool doPath = !String.IsNullOrEmpty(options.Path);
			bool doString = !String.IsNullOrEmpty(options.QueryString);

			for (int i = 0; i < fileLines.Length; i++)
			{
				if (i == 0) Console.WriteLine("Beginning parsing log...");

				if (fileLines[i].StartsWith("#")) continue;

				string[] parts = fileLines[i].Split(' ');
				string date = parts[0];
				int year = Convert.ToInt16(date.Split('-')[0]);
				int month = Convert.ToInt16(date.Split('-')[1]);
				int day = Convert.ToInt16(date.Split('-')[2]);

				string time = parts[1];
				int hour = Convert.ToInt16(time.Split(':')[0]);
				int minute = Convert.ToInt16(time.Split(':')[1]);
				int second = Convert.ToInt16(time.Split(':')[2]);

				DateTime lineTime = new DateTime(year, month, day, hour, minute, second);

				if (doStartDate && DateTime.Compare(lineTime, startDate) <= 0)
				{
					continue;
				}

				if (doEndDate && DateTime.Compare(lineTime, endDate) >= 0)
				{
					break;
				}

				if (doPath && !parts[4].Contains(options.Path))
				{
					continue;
				}

				if (doString && !parts[5].ToUpper().Contains(options.QueryString.ToUpper()))
				{
					continue;
				}

				Console.WriteLine(fileLines[i]);
			}
		}

		static void parseLog(Options options, FileStream outFs)
		{
			string[] fileLines = File.ReadAllLines(options.FilePath);

			DateTime startDate;
			DateTime endDate;
			int offset = 0;

			bool doStartDate = DateTime.TryParse(options.StartDateTime, out startDate);
			bool doEndDate = DateTime.TryParse(options.StartDateTime, out endDate);
			bool doPath = !String.IsNullOrEmpty(options.Path);
			bool doString = !String.IsNullOrEmpty(options.QueryString);

			for (int i = 0; i < fileLines.Length; i++)
			{
				if (i == 0) Console.WriteLine("Beginning parsing log...");

				string[] parts = fileLines[i].Split(' ');
				string date = parts[0];
				int year = Convert.ToInt16(date.Split('-')[0]);
				int month = Convert.ToInt16(date.Split('-')[1]);
				int day = Convert.ToInt16(date.Split('-')[2]);

				string time = parts[1];
				int hour = Convert.ToInt16(time.Split(':')[0]);
				int minute = Convert.ToInt16(time.Split(':')[1]);
				int second = Convert.ToInt16(time.Split(':')[2]);

				DateTime lineTime = new DateTime(year, month, day, hour, minute, second);

				if (doStartDate && DateTime.Compare(lineTime, startDate) <= 0)
				{
					continue;
				}

				if (doEndDate && DateTime.Compare(lineTime, endDate) >= 0)
				{
					break;
				}

				if (doPath && !parts[4].ToUpper().Contains(options.Path.ToUpper()))
				{
					continue;
				}

				if (doString && !parts[5].ToUpper().Contains(options.QueryString.ToUpper()))
				{
					continue;
				}

				byte[] writeBytes = Encoding.ASCII.GetBytes(fileLines[i]);
				outFs.Write(writeBytes, offset, writeBytes.Length);
				offset += writeBytes.Length;
			}

			if (outFs != null) outFs.Close();
		}

		private static void GetPrintFileSize(String path)
		{
			long fileBytes = new FileInfo(path).Length;

			int exponent = 1;
			int normalisedSize = Convert.ToInt32(Math.Floor(fileBytes / (Math.Pow(1024, exponent))));
			string sizeDescriptor = "bytes";

			while (fileBytes / (Math.Pow(1024, exponent)) > 1 && exponent <= 4)
			{
				normalisedSize = Convert.ToInt32(Math.Floor(fileBytes / (Math.Pow(1024, exponent))));
				exponent++;
			}

			switch (exponent)
			{
				case 2:
					sizeDescriptor = "kilo" + sizeDescriptor;
					break;

				case 3:
					sizeDescriptor = "mega" + sizeDescriptor;
					break;

				case 4:
					sizeDescriptor = "tera" + sizeDescriptor;
					break;

				default:
					break;
			}

			Console.WriteLine("Parsing '" + path + "', a " + normalisedSize + " " + sizeDescriptor + " file.");
		}
	}

	class Options
	{
		[Option('f', "file", Required = true, HelpText = "File path of IIS log")]
		public String FilePath { get; set; }

		[Option('s', "starttime", Required = false, HelpText = "Start time in ISO format, e.g. \"2016-11-27T18:00:00\" to default to local timezone, or \"2016-11-27T18:00:00.000+0100Z\" to specify a timezone.")]
		public String StartDateTime { get; set; }

		[Option('e', "endtime", Required = false, HelpText = "End time in ISO format, see \"Start time\" for details.")]
		public String EndDateTime { get; set; }

		[Option('p', "path", Required = false, HelpText = "Path/endpoint calls for which to query the log file, e.g. \"/api/customers\", which will return any log row that contains \"/api/customers\". Note that if both the 'path' option and the 'querystring' option are specified, only log rows that match both are returned.")]
		public String Path { get; set; }

		[Option('q', "querystring", Required = false, HelpText = "Query string parameters for which to query the log file, e.g. \"id=10\", which will return any row that contains \"id=10\". Note that if both the 'path' option and the 'querystring' option are specified, only log rows that match both are returned.")]
		public String QueryString { get; set; }

		[Option('o', "outputfile", Required = false, HelpText = "Path to write the results as a new file. If unspecified, logs are written to the current window.")]
		public String OutputFilePath { get; set; }

		[ParserState]
		public IParserState LastParserState { get; set; }

		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this,
			  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}
}
