using System.Threading.Tasks;
 
 namespace AElf.Kernel
 {
     public interface ITransactionExecutingManager
     {
         Task ExecuteAsync(ITransaction tx);
     }
 }