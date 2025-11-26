import React, { useEffect, useState } from 'react';
import { AppConfig, TunnelConfig } from '@shared/types/Configuration';
import { v4 as uuidv4 } from 'uuid';

function App() {
  const [config, setConfig] = useState<AppConfig | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [editingTunnel, setEditingTunnel] = useState<Partial<TunnelConfig>>({});

  const [tunnelStatuses, setTunnelStatuses] = useState<Record<string, string>>({});

  useEffect(() => {
    loadConfig();
    const interval = setInterval(updateStatuses, 2000);
    return () => clearInterval(interval);
  }, []);

  const updateStatuses = async () => {
    if (!config) return;
    const statuses: Record<string, string> = {};
    for (const t of config.tunnels) {
      statuses[t.id] = await window.electronAPI.getTunnelStatus(t.id);
    }
    setTunnelStatuses(statuses);
  };

  const loadConfig = async () => {
    const cfg = await window.electronAPI.getConfig();
    setConfig(cfg);
    // Initial status check
    if (cfg) {
      const statuses: Record<string, string> = {};
      for (const t of cfg.tunnels) {
        statuses[t.id] = await window.electronAPI.getTunnelStatus(t.id);
      }
      setTunnelStatuses(statuses);
    }
  };

  const handleToggleTunnel = async (id: string) => {
    const status = tunnelStatuses[id];
    if (status === 'running') {
      await window.electronAPI.stopTunnel(id);
    } else {
      await window.electronAPI.startTunnel(id);
    }
    updateStatuses();
  };

  const getTunnelTypeLabel = (type: string) => {
    switch (type) {
      case 'typescript': return 'Hybrid Connection (Node.js) - Beta';
      case 'dotnet-core': return 'Hybrid Connection (.NET)';
      case 'dotnet-wcf': return 'WCF (.NET Framework) - Legacy';
      default: return type;
    }
  };

  const handleSave = async () => {
    if (!config) return;
    await window.electronAPI.saveConfig(config);
    alert('Configuration saved!');
  };

  const handleAddTunnel = () => {
    setEditingTunnel({
      id: uuidv4(),
      type: 'typescript',
      targetHost: 'localhost',
      targetPort: 8080,
      keyName: 'RootManageSharedAccessKey'
    });
    setIsEditing(true);
  };

  const handleSaveTunnel = async () => {
    if (!config || !editingTunnel.name) return;

    let encryptedKey = editingTunnel.encryptedKey;
    if (editingTunnel.encryptedKey && !editingTunnel.encryptedKey.startsWith('AQAA')) {
         // If it doesn't look like a DPAPI blob (which starts with AQAA usually), assume it's plain text and encrypt it
         // This is a naive check, but good enough for GUI entry
         encryptedKey = await window.electronAPI.encryptKey(editingTunnel.encryptedKey);
    }

    const newTunnel = {
        ...editingTunnel,
        encryptedKey
    } as TunnelConfig;

    const newTunnels = config.tunnels.filter(t => t.id !== newTunnel.id);
    newTunnels.push(newTunnel);

    const newConfig = { ...config, tunnels: newTunnels };
    setConfig(newConfig);
    await window.electronAPI.saveConfig(newConfig);
    setIsEditing(false);
    setEditingTunnel({});
    loadConfig();
  };

  const handleDeleteTunnel = async (id: string) => {
    if (confirm('Are you sure you want to delete this tunnel?')) {
      // Stop it if it's running
      if (tunnelStatuses[id] === 'running') {
        await window.electronAPI.stopTunnel(id);
      }
      await window.electronAPI.deleteTunnel(id);
      loadConfig();
    }
  };

  if (!config) return <div>Loading configuration...</div>;

  return (
    <div style={{ fontFamily: 'Segoe UI, sans-serif', padding: '20px', maxWidth: '800px', margin: '0 auto' }}>
      <h1>Azure Relay Tunneling Utility</h1>
      
      {isEditing ? (
        <div style={{ border: '1px solid #ccc', padding: '20px', borderRadius: '4px' }}>
          <h3>{editingTunnel.id ? 'Edit Tunnel' : 'New Tunnel'}</h3>
          <div style={{ display: 'grid', gap: '10px' }}>
            <label>
              Name:
              <input style={{width: '100%'}} value={editingTunnel.name || ''} onChange={e => setEditingTunnel({...editingTunnel, name: e.target.value})} />
            </label>
            <label>
              Type:
              <select style={{width: '100%'}} value={editingTunnel.type || 'typescript'} onChange={e => setEditingTunnel({...editingTunnel, type: e.target.value as any})}>
                <option value="typescript">Hybrid Connection (Node.js) - Beta</option>
                <option value="dotnet-core">Hybrid Connection (.NET)</option>
                <option value="dotnet-wcf">WCF (.NET Framework) - Legacy</option>
              </select>
            </label>
            <label>
              Relay Namespace:
              <input style={{width: '100%'}} value={editingTunnel.relayNamespace || ''} onChange={e => setEditingTunnel({...editingTunnel, relayNamespace: e.target.value})} />
            </label>
            <label>
              Hybrid Connection Name:
              <input style={{width: '100%'}} value={editingTunnel.hybridConnectionName || ''} onChange={e => setEditingTunnel({...editingTunnel, hybridConnectionName: e.target.value})} />
            </label>
            <label>
              Key Name:
              <input style={{width: '100%'}} value={editingTunnel.keyName || ''} onChange={e => setEditingTunnel({...editingTunnel, keyName: e.target.value})} />
            </label>
            <label>
              Key (Secret):
              <input style={{width: '100%'}} type="password" value={editingTunnel.encryptedKey || ''} onChange={e => setEditingTunnel({...editingTunnel, encryptedKey: e.target.value})} placeholder="Enter plain text key to update" />
            </label>
            <div style={{ display: 'flex', gap: '10px' }}>
                <label style={{flex: 1}}>
                    Target Host:
                    <input style={{width: '100%'}} value={editingTunnel.targetHost || ''} onChange={e => setEditingTunnel({...editingTunnel, targetHost: e.target.value})} />
                </label>
                <label style={{width: '100px'}}>
                    Target Port:
                    <input style={{width: '100%'}} type="number" value={editingTunnel.targetPort || 0} onChange={e => setEditingTunnel({...editingTunnel, targetPort: parseInt(e.target.value)})} />
                </label>
            </div>

            <label>
              Description:
              <input style={{width: '100%'}} value={editingTunnel.description || ''} onChange={e => setEditingTunnel({...editingTunnel, description: e.target.value})} />
            </label>
            <label style={{display: 'flex', alignItems: 'center', gap: '10px'}}>
              <input type="checkbox" checked={editingTunnel.enableDetailedLogging || false} onChange={e => setEditingTunnel({...editingTunnel, enableDetailedLogging: e.target.checked})} />
              Detailed Logging
            </label>
            <label style={{display: 'flex', alignItems: 'center', gap: '10px'}}>
              <input type="checkbox" checked={editingTunnel.requiresClientAuthorization || false} onChange={e => setEditingTunnel({...editingTunnel, requiresClientAuthorization: e.target.checked})} />
              Requires Client Authorization
            </label>

            {editingTunnel.type === 'dotnet-core' && (
                <>
                    <label style={{display: 'flex', alignItems: 'center', gap: '10px'}}>
                      <input type="checkbox" checked={editingTunnel.enableWebSocketSupport || false} onChange={e => setEditingTunnel({...editingTunnel, enableWebSocketSupport: e.target.checked})} />
                      Enable WebSocket Support
                    </label>
                    {editingTunnel.enableWebSocketSupport && (
                        <label>
                            Target WebSocket Address:
                            <input style={{width: '100%'}} value={editingTunnel.targetWebSocketAddress || ''} onChange={e => setEditingTunnel({...editingTunnel, targetWebSocketAddress: e.target.value})} />
                        </label>
                    )}
                </>
            )}

            {editingTunnel.type === 'dotnet-wcf' && (
                <label>
                    Service Discovery Mode:
                    <select style={{width: '100%'}} value={editingTunnel.serviceDiscoveryMode || 'Private'} onChange={e => setEditingTunnel({...editingTunnel, serviceDiscoveryMode: e.target.value as any})}>
                        <option value="Private">Private</option>
                        <option value="Public">Public</option>
                    </select>
                </label>
            )}

            <label style={{display: 'flex', alignItems: 'center', gap: '10px'}}>
              <input type="checkbox" checked={editingTunnel.dynamicResourceCreation || false} onChange={e => setEditingTunnel({...editingTunnel, dynamicResourceCreation: e.target.checked})} />
              Dynamic Resource Creation
            </label>

            {editingTunnel.dynamicResourceCreation && (
                <div style={{border: '1px dashed #999', padding: '10px', borderRadius: '4px', background: '#f0f0f0'}}>
                    <h4 style={{marginTop: 0}}>Azure Management</h4>
                    <label>
                        Resource Group Name:
                        <input style={{width: '100%'}} value={editingTunnel.resourceGroupName || ''} onChange={e => setEditingTunnel({...editingTunnel, resourceGroupName: e.target.value})} />
                    </label>
                    <label>
                        Azure Subscription ID:
                        <input style={{width: '100%'}} value={editingTunnel.azureManagement?.subscriptionId || ''} onChange={e => setEditingTunnel({...editingTunnel, azureManagement: {...(editingTunnel.azureManagement || {}), subscriptionId: e.target.value}})} />
                    </label>
                    <label>
                        Azure Tenant ID:
                        <input style={{width: '100%'}} value={editingTunnel.azureManagement?.tenantId || ''} onChange={e => setEditingTunnel({...editingTunnel, azureManagement: {...(editingTunnel.azureManagement || {}), tenantId: e.target.value}})} />
                    </label>
                    <label style={{display: 'flex', alignItems: 'center', gap: '10px', margin: '10px 0'}}>
                        <input type="checkbox" checked={editingTunnel.azureManagement?.useDefaultAzureCredential ?? true} onChange={e => setEditingTunnel({...editingTunnel, azureManagement: {...(editingTunnel.azureManagement || {}), useDefaultAzureCredential: e.target.checked}})} />
                        Use Default Azure Credential
                    </label>
                    {!(editingTunnel.azureManagement?.useDefaultAzureCredential ?? true) && (
                        <>
                            <label>
                                Client ID:
                                <input style={{width: '100%'}} value={editingTunnel.azureManagement?.clientId || ''} onChange={e => setEditingTunnel({...editingTunnel, azureManagement: {...(editingTunnel.azureManagement || {}), clientId: e.target.value}})} />
                            </label>
                            <label>
                                Client Secret:
                                <input style={{width: '100%'}} type="password" value={editingTunnel.azureManagement?.clientSecret || ''} onChange={e => setEditingTunnel({...editingTunnel, azureManagement: {...(editingTunnel.azureManagement || {}), clientSecret: e.target.value}})} />
                            </label>
                        </>
                    )}
                </div>
            )}

            <div style={{ marginTop: '10px' }}>
                <button onClick={handleSaveTunnel} style={{ marginRight: '10px', padding: '5px 10px' }}>Save</button>
                <button onClick={() => setIsEditing(false)} style={{ padding: '5px 10px' }}>Cancel</button>
            </div>
          </div>
        </div>
      ) : (
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
            <h2>Tunnels</h2>
            <button onClick={handleAddTunnel} style={{ padding: '8px 16px', background: '#0078d4', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>+ Add Tunnel</button>
          </div>
          
          {config.tunnels.length === 0 ? (
            <p>No tunnels configured.</p>
          ) : (
            <div style={{ display: 'grid', gap: '10px' }}>
              {config.tunnels.map(t => (
                <div key={t.id} style={{ border: '1px solid #eee', padding: '15px', borderRadius: '4px', background: '#f9f9f9' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <strong>{t.name}</strong>
                    <span style={{ fontSize: '0.8em', background: '#ddd', padding: '2px 6px', borderRadius: '4px' }}>{getTunnelTypeLabel(t.type)}</span>
                  </div>
                  <div style={{ fontSize: '0.9em', color: '#666', marginTop: '5px' }}>
                    {t.relayNamespace} / {t.hybridConnectionName} → {t.targetHost}:{t.targetPort}
                  </div>
                  <div style={{ marginTop: '10px' }}>
                    <button onClick={() => handleToggleTunnel(t.id)} style={{ marginRight: '10px', padding: '5px 10px', background: tunnelStatuses[t.id] === 'running' ? '#d9534f' : '#28a745', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>
                        {tunnelStatuses[t.id] === 'running' ? 'Stop' : 'Start'}
                    </button>
                    <button onClick={() => { setEditingTunnel(t); setIsEditing(true); }} style={{ padding: '5px 10px', cursor: 'pointer', marginRight: '10px' }} disabled={tunnelStatuses[t.id] === 'running'}>Edit</button>
                    <button onClick={() => handleDeleteTunnel(t.id)} style={{ padding: '5px 10px', cursor: 'pointer', background: '#d9534f', color: 'white', border: 'none', borderRadius: '4px' }} disabled={tunnelStatuses[t.id] === 'running'}>Delete</button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

export default App;

