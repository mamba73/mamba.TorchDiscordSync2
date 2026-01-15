U konfiguraciji za Discord bot je postavljeno samo:
<?xml version="1.0"?>
<DiscordBotConfig xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <BotToken>YOUR_DISCORD_BOT_TOKEN</BotToken>
  <GuildID>0</GuildID>
  <BotPrefix>!</BotPrefix>
  <EnableDMNotifications>true</EnableDMNotifications>
  <VerificationCodeExpirationMinutes>15</VerificationCodeExpirationMinutes>
</DiscordBotConfig>

Nedostaje **konfiguracija** za:
* <CategorylId> - gdje će se kreirati forumi za fakcije po imenu fakcije iz SE igre - sjeti se da sam to bio spominjao.
* <ChatChannelId> - gdje će se slati informacije tko se spojio / odspojio sa servera, te death poruke. To bi bio i kanal koji se sinhronizira obostrano, na discordu se vide poruke koje su pisane u public chatu unutar igre i obrnuto.
Staviti boolean za uključen/isključen chat u jednom ili drugom smjeru:
  <BotToGame>true</BotToGame>
  <ServerToDiscord>true</ServerToDiscord>


Format poruka iz igre SE prema discordu:
  <Format>:rocket: **{p}**: {msg}</Format>
Format poruka iz discorda za SE:
  <Format2>[SE]{p}</Format2>

Format join/leave poruka:
  <Connect>:key: {p} connected to server</Connect>
  <Join>:sunny: {p} joined the server</Join>
  <Leave>:new_moon: {p} left the server</Leave>

Staff dio konfiguracije:
* <StaffLog> - gdje će se spremati informacije o kreiranim rolama, forumima i imenima korisnika na discordu
* <StatusChannelId> - gdje će se slati upozorenja o sim speed i kada se server podigne ili ugasi
Poruke u obliku:
  <Started>:white_check_mark: Server Started!</Started>
  <Stopped>:x: Server Stopped!</Stopped>
  <Restarted>:arrows_counterclockwise: Server Restarted!</Restarted>

Dodati i implementirati mogućnost za simping:
  <SimPing>true</SimPing>
  <SimChannel>ChannelID</SimChannel>
  <SimThresh>0.6</SimThresh>
  <SimMessage>@here Simulation speed has dropped below threshold!</SimMessage>
  <SimCooldown>1200</SimCooldown>
  <UseStatus>true</UseStatus>
  <StatusInterval>5000</StatusInterval>
  <StatusPre>Server Starting...</StatusPre>
  <Status>{p} players | SS {ss}</Status>

Novi feature:
Postoji li mogućnost prilikom kreiranja foruma, kreirati i voice kanal i kategoriju po imenu fakcije, gdje bi se slao chat unutar fakcije, te sinhronizirao obostrano: sa discorda u igru i iz igre na discord? Ako da, napraviti konfiguraciju:
  <UseFactionChat>true</UseFactionChat>
  <FacFormat>:ledger: **{p}**: {msg}</FacFormat>
  <FacFormat2>[SE-Fac]{p}</FacFormat2>

Također bi trebalo definirati boje:

  <GlobalColor>White</GlobalColor>
  <FacColor>Green</FacColor>
kada se piše sa discorda kako bi se razlikovalo ide li poruka u fakciju ili globalno.
