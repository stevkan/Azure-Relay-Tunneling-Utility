import { z } from 'zod'

export const AzureManagementConfigSchema = z.object({
  subscriptionId: z.string().optional(),
  tenantId: z.string().optional(),
  clientId: z.string().optional(),
  clientSecret: z.string().optional(),
  useDefaultAzureCredential: z.boolean().default(true)
})

export const RelayConfigSchema = z.object({
  relayNamespace: z.string().min(1, 'RelayNamespace is required'),
  relayName: z.string().min(1, 'RelayName is required').transform(val => val.toLowerCase()),
  policyName: z.string().min(1, 'PolicyName is required'),
  policyKey: z.string().min(1, 'PolicyKey is required'),
  targetServiceAddress: z.string().url('TargetServiceAddress must be a valid URL'),
  isEnabled: z.boolean().default(true),
  enableDetailedLogging: z.boolean().default(true),
  dynamicResourceCreation: z.boolean().default(false),
  resourceGroupName: z.string().optional(),
  requiresClientAuthorization: z.boolean().default(true),
  description: z.string().optional()
})

export const AppSettingsSchema = z.object({
  azureManagement: AzureManagementConfigSchema.optional(),
  relays: z.array(RelayConfigSchema).min(1, 'At least one relay configuration is required'),
  shutdownTimeoutSeconds: z.number().int().positive().default(30)
})

export type AzureManagementConfig = z.infer<typeof AzureManagementConfigSchema>
export type RelayConfig = z.infer<typeof RelayConfigSchema>
export type AppSettings = z.infer<typeof AppSettingsSchema>

export interface RelayConfigWithOriginalName extends RelayConfig {
  originalRelayName?: string
}

function parseBooleanEnv(value: string | undefined, defaultValue: boolean): boolean {
  if (!value) return defaultValue
  return value.toLowerCase() === 'true' || value === '1'
}

export function loadConfigFromEnv(): AppSettings {
  const azureManagement: AzureManagementConfig = {
    subscriptionId: process.env.AZURE_SUBSCRIPTION_ID,
    tenantId: process.env.AZURE_TENANT_ID,
    clientId: process.env.AZURE_CLIENT_ID,
    clientSecret: process.env.AZURE_CLIENT_SECRET,
    useDefaultAzureCredential: parseBooleanEnv(process.env.AZURE_USE_DEFAULT_CREDENTIAL, true)
  }

  const relaysEnv = process.env.RELAYS
  if (!relaysEnv) {
    throw new Error('RELAYS environment variable is required')
  }

  const relays: RelayConfig[] = relaysEnv.split(';').map((relayStr) => {
    const parts = relayStr.split('|')
    if (parts.length < 5) {
      throw new Error(`Invalid relay configuration: ${relayStr}. Expected format: namespace|name|policyName|policyKey|targetUrl|enabled|detailedLogging|dynamic|resourceGroup|requiresAuth|description`)
    }

    return {
      relayNamespace: parts[0].trim(),
      relayName: parts[1].trim(),
      policyName: parts[2].trim(),
      policyKey: parts[3].trim(),
      targetServiceAddress: parts[4].trim(),
      isEnabled: parts[5] ? parseBooleanEnv(parts[5].trim(), true) : true,
      enableDetailedLogging: parts[6] ? parseBooleanEnv(parts[6].trim(), true) : true,
      dynamicResourceCreation: parts[7] ? parseBooleanEnv(parts[7].trim(), false) : false,
      resourceGroupName: parts[8]?.trim() || undefined,
      requiresClientAuthorization: parts[9] ? parseBooleanEnv(parts[9].trim(), true) : true,
      description: parts[10]?.trim() || undefined
    }
  })

  const shutdownTimeoutSeconds = process.env.SHUTDOWN_TIMEOUT_SECONDS
    ? parseInt(process.env.SHUTDOWN_TIMEOUT_SECONDS, 10)
    : 30

  return AppSettingsSchema.parse({
    azureManagement,
    relays,
    shutdownTimeoutSeconds
  })
}
