#region using
using System;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Xsl;
#endregion

namespace XsltCancellation
{
	static class Program
	{
		private static void Main(string[] args)
		{
			if (args.Length<3)
			{
				Console.WriteLine($"Usage: {nameof(XsltCancellation)} transform.xslt data.xml outputfile.ext");
				return;
			}

			XslCompiledTransform xslCompiledTransform = new XslCompiledTransform(false);
			xslCompiledTransform.Load(args[0]);

			using (CancellationTokenSource cts = new CancellationTokenSource())
			{
				cts.CancelAfter(5_000);
				Console.CancelKeyPress+=(sender,e) => { e.Cancel=true; cts.Cancel(); };
				Console.WriteLine("Starting...");

				try
				{
					using (XmlWriter xw = new XmlTextWriter(args[2],Encoding.UTF8))
						xslCompiledTransform.Transform(args[1],new CancellableXmlWriter(xw,cts.Token,false));

					Console.WriteLine("Done");
				}
				catch (OperationCanceledException)
				{
					Console.WriteLine("Canceled");
				}
			}
		}
	}
}