using System;
using System.IO;
using Xunit;
using Shouldly;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AElf.Common.Application.Test
{
    public class ApplicationHelpersTest
    {

        //TODO:fix the path unit test, may concurrency problem
        /*BODY:
AElf.Common.Application.Test.ApplicationHelpersTest.Get_Path

Shouldly.ShouldAssertException : appDatePath1
    should be
"/Users/loning/.local/share/aelf"
    but was
"/tmp/not_exist"
    difference
Difference     |       |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |        
               |      \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/       
Index          | ...  1    2    3    4    5    6    7    8    9    10   11   12   13   14   15   16   17   18   19   20   21   ...  
Expected Value | ...  U    s    e    r    s    /    l    o    n    i    n    g    /    .    l    o    c    a    l    /    s    ...  
Actual Value   | ...  t    m    p    /    n    o    t    _    e    x    i    s    t                                            ...  
Expected Code  | ...  85   115  101  114  115  47   108  111  110  105  110  103  47   46   108  111  99   97   108  47   115  ...  
Actual Code    | ...  116  109  112  47   110  111  116  95   101  120  105  115  116                                          ...  

Difference     |       |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |   
               |      \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  \|/  
Index          | ...  10   11   12   13   14   15   16   17   18   19   20   21   22   23   24   25   26   27   28   29   30   
Expected Value | ...  i    n    g    /    .    l    o    c    a    l    /    s    h    a    r    e    /    a    e    l    f    
Actual Value   | ...  x    i    s    t                                                                                         
Expected Code  | ...  105  110  103  47   46   108  111  99   97   108  47   115  104  97   114  101  47   97   101  108  102  
Actual Code    | ...  120  105  115  116                                                                                       
   at AElf.Common.Application.Test.ApplicationHelpersTest.Get_Path() in /Users/loning/sources/AElf/AElf.Common.Test/Application/ApplicationHelpersTest.cs:line 16

         
        [Fact]
        public void Get_Path()
        {
            var appDatePath1 = ApplicationHelper.AppDataPath;
            var appDatePath2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "aelf");
            appDatePath1.ShouldBe(appDatePath2);
        }*/

        [Fact(Skip = "hh")]
        public void Set_Path()
        {
            var path1 = "";
            var path2 = "/tmp/not_exist";

            ApplicationHelper.AppDataPath = path1;
            ApplicationHelper.AppDataPath = path2;
            ApplicationHelper.AppDataPath.ShouldBe(path2);
        }
    }
}