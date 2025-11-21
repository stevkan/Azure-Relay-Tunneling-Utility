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
                <option value="typescript">TypeScript (Node.js)</option>
                <option value="dotnet-core">.NET 8</option>
                <option value="dotnet-wcf">.NET Framework 4.8 (WCF)</option>
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
                    <span style={{ fontSize: '0.8em', background: '#ddd', padding: '2px 6px', borderRadius: '4px' }}>{t.type}</span>
                  </div>
                  <div style={{ fontSize: '0.9em', color: '#666', marginTop: '5px' }}>
                    {t.relayNamespace} / {t.hybridConnectionName} → {t.targetHost}:{t.targetPort}
                  </div>
                  <div style={{ marginTop: '10px' }}>
                    <button onClick={() => handleToggleTunnel(t.id)} style={{ marginRight: '10px', padding: '5px 10px', background: tunnelStatuses[t.id] === 'running' ? '#d9534f' : '#28a745', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>
                        {tunnelStatuses[t.id] === 'running' ? 'Stop' : 'Start'}
                    </button>
                    <button onClick={() => { setEditingTunnel(t); setIsEditing(true); }} style={{ padding: '5px 10px', cursor: 'pointer' }} disabled={tunnelStatuses[t.id] === 'running'}>Edit</button>
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

