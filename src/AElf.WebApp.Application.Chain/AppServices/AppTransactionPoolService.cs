using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AElf.WebApp.Application.Chain.Dto;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain.AppServices
{
    public interface IAppTransactionPoolService : IApplicationService
    {
        /// <summary>
        /// Get the transaction pool status.
        /// </summary>
        /// <returns></returns>
        Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatusAsync();
    }

    /// <summary>
    /// transaction pool services
    /// </summary>
    /// <seealso cref="Object" />
    /// <seealso cref="AElf.WebApp.Application.Chain.AppServices.IAppTransactionPoolService" />
    public class AppTransactionPoolService : IAppTransactionPoolService
    {
        private readonly ITxHub _txHub;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppTransactionPoolService"/> class.
        /// </summary>
        /// <param name="txHub">The tx hub.</param>
        public AppTransactionPoolService(ITxHub txHub)
        {
            _txHub = txHub;
        }

        #region transaction pool

        /// <summary>
        /// Get the transaction pool status.
        /// </summary>
        /// <returns></returns>
        public async Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatusAsync()
        {
            var queued = await _txHub.GetTransactionPoolSizeAsync();
            return new GetTransactionPoolStatusOutput
            {
                Queued = queued
            };
        }

        #endregion transaction pool
    }
}