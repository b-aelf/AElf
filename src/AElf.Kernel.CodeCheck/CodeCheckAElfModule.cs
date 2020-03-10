using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.CodeCheck
{
    [DependsOn(typeof(ProposalAElfModule))]
    public class CodeCheckAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IBestChainFoundLogEventProcessor, CodeCheckRequiredLogEventProcessor>();
            context.Services.AddSingleton<IContractAuditorContainer, ContractAuditorContainer>();
        }
    }
}