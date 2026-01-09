// Script para gerar chaves VAPID para Web Push Notifications
// Execute: node generate-vapid-keys.js

const webpush = require('web-push');

// Gerar chaves VAPID
const vapidKeys = webpush.generateVAPIDKeys();

console.log('\n=== Chaves VAPID Geradas ===\n');
console.log('Chave Pública (VAPID_PUBLIC_KEY):');
console.log(vapidKeys.publicKey);
console.log('\nChave Privada (VAPID_PRIVATE_KEY):');
console.log(vapidKeys.privateKey);
console.log('\n=== Configuração ===\n');
console.log('Adicione estas variáveis de ambiente no seu backend (.NET):');
console.log(`VAPID_PUBLIC_KEY=${vapidKeys.publicKey}`);
console.log(`VAPID_PRIVATE_KEY=${vapidKeys.privateKey}`);
console.log(`VAPID_SUBJECT=mailto:seu-email@exemplo.com`);
console.log('\nE no frontend (.env):');
console.log(`VITE_VAPID_PUBLIC_KEY=${vapidKeys.publicKey}`);
console.log('\n');
