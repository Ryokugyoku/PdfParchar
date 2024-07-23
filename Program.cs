
using System.IO.Enumeration;
using PdfParchar.Module;

namespace PdfParchar{
    
    class Program{
        readonly static string Input =  @Directory.GetCurrentDirectory()+"/Input";
        readonly static string Output = @Directory.GetCurrentDirectory()+"/Output";
        static void Main(string[] args){
            foreach(string fileName in Directory.GetFiles(Input)){
                PdfManager pdfManager = new PdfManager(fileName, Output);
            }
            Console.WriteLine("OK");
        }
    }
    
}

