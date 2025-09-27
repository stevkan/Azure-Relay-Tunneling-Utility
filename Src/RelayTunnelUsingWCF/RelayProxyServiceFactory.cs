using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace RelayTunnelUsingWCF
{
    public class RelayProxyServiceFactory : IServiceBehavior
    {
        private readonly RelayConfiguration _config;
        
        public RelayProxyServiceFactory(RelayConfiguration config)
        {
            _config = config;
        }

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, 
            System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, 
            BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (var channelDispatcher in serviceHostBase.ChannelDispatchers)
            {
                var dispatcher = channelDispatcher as ChannelDispatcher;
                if (dispatcher != null)
                {
                    foreach (var endpointDispatcher in dispatcher.Endpoints)
                    {
                        endpointDispatcher.DispatchRuntime.InstanceProvider = new RelayServiceInstanceProvider(_config);
                    }
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }
    }

    public class RelayServiceInstanceProvider : IInstanceProvider
    {
        private readonly RelayConfiguration _config;

        public RelayServiceInstanceProvider(RelayConfiguration config)
        {
            _config = config;
        }

        public object GetInstance(InstanceContext instanceContext)
        {
            return new ConfigurableRelayProxyService(_config);
        }

        public object GetInstance(InstanceContext instanceContext, Message message)
        {
            return new ConfigurableRelayProxyService(_config);
        }

        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
