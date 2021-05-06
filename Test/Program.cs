#region using
extern alias Local;
using System.IO;
using XslCompiledTransform = Local::System.Xml.Xsl.XslCompiledTransform;
using XsltArgumentList = Local::System.Xml.Xsl.XsltArgumentList;
using XsltSettings = Local::System.Xml.Xsl.XsltSettings;
using XmlReader = Local::System.Xml.XmlReader;
using XmlTextReader = Local::System.Xml.XmlTextReader;
using XmlUrlResolver = Local::System.Xml.XmlUrlResolver;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
#endregion using

namespace Test
{
	class Program
	{
		static async Task Main(string[] args)
		{
			string transTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xsl:stylesheet version=""1.0"" exclude-result-prefixes=""xsl f"" xmlns:f=""XslHelperMethods"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"">
	<xsl:output method=""xml"" omit-xml-declaration=""yes"" />

	<xsl:template match=""/"">
<xsl:value-of select=""f:ThreadSleep(2000)""/>
		<neco/>
<xsl:call-template name=""vic""/>
	</xsl:template>

	<xsl:template name=""vic"">
		<vic/>
	</xsl:template>
</xsl:stylesheet>
";

			XslCompiledTransform transform = new XslCompiledTransform(true);
			using (Stream file = new MemoryStream(Encoding.UTF8.GetBytes(transTemplate)))
			using (XmlReader xr = new XmlTextReader(file))
				transform.Load(xr,new XsltSettings(true,false),new XmlUrlResolver());
			
			string s;
			using (TextReader tr = new StringReader("<root/>"))
			using (XmlReader xr = new XmlTextReader(tr))
			using (StringWriter sw = new StringWriter())
			{
				CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
				//cancellationTokenSource.Cancel();
				//cancellationTokenSource.CancelAfter(1000);
				XsltArgumentList xsltArgs = GetXsltArgumentList();
				try
				{
					await transform.TransformAsync(xr,xsltArgs,sw,cancellationTokenSource.Token);
				}
				catch (Exception ex)
				{
					throw;
				}
				s=sw.GetStringBuilder().ToString();
				Console.WriteLine("=======");
				Console.WriteLine(s);
			}
		}

		static XsltArgumentList GetXsltArgumentList()
		{
			XsltArgumentList result = new XsltArgumentList();
			result.AddExtensionObject("XslHelperMethods",new XslHelperMethods());
			return result;
		}

		class XslHelperMethods
		{
			public string ThreadSleep(int milliseconds)
			{
				Console.WriteLine($"Going to sleep for {milliseconds} ms.");
				Thread.Sleep(10);
				Console.WriteLine("Rested.");
				return "";
			}
		}
	}
}
