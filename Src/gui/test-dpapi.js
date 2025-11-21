const dpapi = require('@primno/dpapi');
console.log('Is supported:', dpapi.isPlatformSupported);
try {
    const buffer = Buffer.from('test');
    const encrypted = dpapi.Dpapi.protectData(buffer, null, 'CurrentUser');
    console.log('Encryption successful');
} catch (e) {
    console.error('Encryption failed:', e);
    if (dpapi.Dpapi.error) {
        console.error('Inner error:', dpapi.Dpapi.error);
    }
}
