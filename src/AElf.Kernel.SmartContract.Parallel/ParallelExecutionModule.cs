using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Parallel.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using AElf.Kernel.SmartContract.Orleans;

namespace AElf.Kernel.SmartContract.Parallel;

[DependsOn(typeof(SmartContractAElfModule),typeof(SiloExecutionAElfModule))]
public class ParallelExecutionModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IBlockExecutingService, BlockParallelExecutingService>();
        context.Services
            .AddSingleton<IParallelTransactionExecutingService, LocalParallelTransactionExecutingService>();
        context.Services.AddSingleton<ITransactionExecutingService, LocalParallelTransactionExecutingService>();
        if (true)
        {
             //context.Services.AddApplication<SiloExecutionAElfModule>();
            // context.Services.AddAbpModule<SiloExecutionAElfModule>();
        }

    }
}