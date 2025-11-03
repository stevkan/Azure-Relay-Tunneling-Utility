import { RelayAPI } from '@azure/arm-relay'
import { DefaultAzureCredential, ClientSecretCredential } from '@azure/identity'
import type { AzureManagementConfig, RelayConfig } from './config'
import { Logger } from 'pino'

export class RelayResourceManager {
  private readonly client: RelayAPI
  private readonly subscriptionId: string

  constructor(config: AzureManagementConfig, private logger: Logger) {
    if (!config.subscriptionId) {
      throw new Error('SubscriptionId is required for RelayResourceManager')
    }

    this.subscriptionId = config.subscriptionId

    const credential = config.useDefaultAzureCredential
      ? new DefaultAzureCredential()
      : new ClientSecretCredential(
          config.tenantId!,
          config.clientId!,
          config.clientSecret!
        )

    this.client = new RelayAPI(credential, this.subscriptionId)
  }

  async createHybridConnectionAsync(config: RelayConfig): Promise<boolean> {
    try {
      const namespaceShortName = this.getNamespaceFromFqdn(config.relayNamespace)

      if (!config.resourceGroupName) {
        throw new Error('ResourceGroupName is required for dynamic resource creation')
      }

      const hybridConnectionData = {
        requiresClientAuthorization: config.requiresClientAuthorization,
        userMetadata: config.description
      }

      await this.client.hybridConnections.createOrUpdate(
        config.resourceGroupName,
        namespaceShortName,
        config.relayName,
        hybridConnectionData
      )

      return true
    } catch (error: any) {
      if (error.statusCode === 409 || error.code === 'Conflict') {
        return true
      }
      this.logger.error(`Error creating '${config.relayName}': ${error.message}`)
      return false
    }
  }

  async deleteHybridConnectionAsync(config: RelayConfig): Promise<boolean> {
    try {
      this.logger.info(`Deleting '${config.relayName}'...`)

      const namespaceShortName = this.getNamespaceFromFqdn(config.relayNamespace)

      if (!config.resourceGroupName) {
        throw new Error('ResourceGroupName is required for dynamic resource deletion')
      }

      await this.client.hybridConnections.delete(
        config.resourceGroupName,
        namespaceShortName,
        config.relayName
      )

      this.logger.info(`✓ Deleted '${config.relayName}'`)
      return true
    } catch (error: any) {
      if (error.statusCode === 404 || error.code === 'ResourceNotFound') {
        return true
      }
      this.logger.error(`Error deleting '${config.relayName}': ${error.message}`)
      return false
    }
  }

  async hybridConnectionExistsAsync(config: RelayConfig): Promise<boolean> {
    try {
      const namespaceShortName = this.getNamespaceFromFqdn(config.relayNamespace)

      if (!config.resourceGroupName) {
        return false
      }

      await this.client.hybridConnections.get(
        config.resourceGroupName,
        namespaceShortName,
        config.relayName
      )

      return true
    } catch (error: any) {
      if (error.statusCode === 404 || error.code === 'ResourceNotFound') {
        return false
      }
      this.logger.warn(`Error checking Hybrid Connection existence: ${error.message}`)
      return false
    }
  }

  private getNamespaceFromFqdn(namespaceValue: string): string {
    if (namespaceValue.includes('.servicebus.windows.net')) {
      return namespaceValue.replace('.servicebus.windows.net', '')
    }
    return namespaceValue
  }
}
