using System;
using System.IO;
using System.CodeDom.Compiler;
using System.Reflection;

namespace EC.MS 
{
    class Injector 
    {
        public static void LoadAssembly()
        {
            FileInfo srcFile = new FileInfo(Variables.MAIN_FILE + ".cs");

            Console.WriteLine(srcFile.Exists);
        }
    }
}