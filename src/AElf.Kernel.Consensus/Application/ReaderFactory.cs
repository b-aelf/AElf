using AElf.Kernel.SmartContract.Application;
using Acs4;

namespace AElf.Kernel.Consensus.Application
{
    
    //TODO!! why not implement a IReaderFactory<T>?
    internal interface IConsensusReaderFactory
    {
        ConsensusContractContainer.ConsensusContractStub Create(IChainContext chainContext);
    }

    internal class ConsensusReaderFactory : IConsensusReaderFactory
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IConsensusReaderContextService _contextService;

        public ConsensusReaderFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressService smartContractAddressService, IConsensusReaderContextService contextService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
            _contextService = contextService;
        }

        public ConsensusContractContainer.ConsensusContractStub Create(IChainContext chainContext)
        {
            return new ConsensusContractContainer.ConsensusContractStub
            {
                __factory = new MethodStubFactory(_transactionReadOnlyExecutionService,
                    _smartContractAddressService,
                    chainContext,
                    _contextService)
            };
        }
    }
}