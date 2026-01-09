const os = require('os');

function getLocalIP() {
  const interfaces = os.networkInterfaces();
  const addresses = [];

  for (const name of Object.keys(interfaces)) {
    for (const iface of interfaces[name]) {
      // Ignora endereços internos e não-IPv4
      if (iface.family === 'IPv4' && !iface.internal) {
        addresses.push({
          interface: name,
          address: iface.address
        });
      }
    }
  }

  return addresses;
}

function displayURLs() {
  const ips = getLocalIP();
  
  console.log('\n🌐 URLs de Acesso Local:\n');
  console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n');
  
  if (ips.length === 0) {
    console.log('⚠️  Nenhum IP local encontrado.\n');
    return;
  }

  // Mostra localhost primeiro
  console.log('📍 Local (localhost):');
  console.log(`   Frontend: http://localhost:3000`);
  console.log(`   Backend:  http://localhost:5285`);
  console.log(`   API:      http://localhost:5285/api\n`);

  // Mostra IPs da rede local
  console.log('🌍 Rede Local (acessível de outros dispositivos):\n');
  
  ips.forEach((ip, index) => {
    console.log(`   ${index === 0 ? '→' : ' '} Interface: ${ip.interface}`);
    console.log(`     Frontend: http://${ip.address}:3000`);
    console.log(`     Backend:  http://${ip.address}:5285`);
    console.log(`     API:      http://${ip.address}:5285/api`);
    if (index < ips.length - 1) console.log('');
  });

  console.log('\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n');
  
  if (ips.length > 0) {
    console.log(`💡 Use o IP: ${ips[0].address} para acessar de outros dispositivos na mesma rede\n`);
  }
}

displayURLs();
