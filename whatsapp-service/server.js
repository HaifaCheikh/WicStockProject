const express = require("express");
const QRCode = require("qrcode");
const {
  default: makeWASocket,
  useMultiFileAuthState,
  DisconnectReason,
} = require("@whiskeysockets/baileys");

const app = express();
app.use(express.json());

const PORT = process.env.PORT || 3001;

let sock = null;
let qrCodeData = null;
let connectionStatus = "DISCONNECTED";

async function connectToWhatsApp() {
  try {
    const { state, saveCreds } = await useMultiFileAuthState("auth_info_baileys");

    sock = makeWASocket({
      auth: state,
      printQRInTerminal: false,
    });

    sock.ev.on("creds.update", saveCreds);

    sock.ev.on("connection.update", async (update) => {
      const { connection, lastDisconnect, qr } = update;

      if (qr) {
        qrCodeData = await QRCode.toDataURL(qr);
        console.log("[WHATSAPP] NOUVEAU QR CODE DISPONIBLE À L'URL /qr");
      }

      if (connection === "close") {
        connectionStatus = "DISCONNECTED";
        const statusCode = lastDisconnect?.error?.output?.statusCode;
        const shouldReconnect = statusCode !== DisconnectReason.loggedOut;
        console.log(`[WHATSAPP] Connexion fermée (code: ${statusCode}). Reconnexion: ${shouldReconnect}`);
        if (shouldReconnect) {
          setTimeout(connectToWhatsApp, 5000);
        }
      } else if (connection === "open") {
        connectionStatus = "CONNECTED";
        qrCodeData = null;
        console.log("[WHATSAPP] CONNECTÉ ET PRÊT À ENVOYER DES MESSAGES !");
      }
    });
  } catch (err) {
    console.error("[WHATSAPP CONNECTION ERROR]", err);
    setTimeout(connectToWhatsApp, 5000);
  }
}

connectToWhatsApp();

app.get("/", (req, res) => {
  res.send(`
    <!DOCTYPE html>
    <html>
      <head>
        <title>WicStock WhatsApp Service</title>
        <meta charset="utf-8">
        <style>
          body { font-family: sans-serif; text-align: center; padding: 40px; background: #f8fafc; color: #1e293b; }
          .card { background: white; max-width: 480px; margin: 0 auto; padding: 30px; border-radius: 16px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }
          .status { font-weight: bold; color: ${connectionStatus === "CONNECTED" ? "#16a34a" : "#dc2626"}; }
          .btn { display: inline-block; padding: 12px 20px; background: #25d366; color: white; border-radius: 8px; text-decoration: none; font-weight: bold; margin-top: 15px; }
        </style>
      </head>
      <body>
        <div class="card">
          <h2>Statut WhatsApp WicStock</h2>
          <p>État : <span class="status">${connectionStatus}</span></p>
          ${
            connectionStatus === "CONNECTED"
              ? '<p style="color: #16a34a;">✅ Service connecté et prêt à transmettre les SMS/OTP.</p>'
              : '<p style="color: #d97706;">⏳ Scannez le QR code pour connecter votre compte WhatsApp.</p><a href="/qr" class="btn">Afficher le QR Code</a>'
          }
        </div>
      </body>
    </html>
  `);
});

app.get("/qr", (req, res) => {
  if (connectionStatus === "CONNECTED") {
    return res.send(`
      <body style="font-family: sans-serif; text-align: center; padding: 40px;">
        <h2 style="color: #16a34a;">✅ Déjà connecté à WhatsApp !</h2>
        <p>Le service est actif et prêt.</p>
        <a href="/">Retour à l'accueil</a>
      </body>
    `);
  }

  if (!qrCodeData) {
    return res.send(`
      <body style="font-family: sans-serif; text-align: center; padding: 40px;">
        <h2>⏳ Génération du QR Code en cours...</h2>
        <p>Veuillez rafraîchir la page dans 3 secondes.</p>
        <script>setTimeout(() => location.reload(), 3000);</script>
      </body>
    `);
  }

  res.send(`
    <!DOCTYPE html>
    <html>
      <head>
        <title>Connexion WhatsApp WicStock</title>
        <meta charset="utf-8">
        <style>
          body { font-family: sans-serif; text-align: center; padding: 30px; background: #f8fafc; }
          .card { background: white; max-width: 440px; margin: 0 auto; padding: 30px; border-radius: 16px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }
          img { border: 4px solid #25d366; border-radius: 12px; }
        </style>
      </head>
      <body>
        <div class="card">
          <h2 style="color: #0f172a;">Scannez ce QR Code avec WhatsApp</h2>
          <p style="color: #64748b; font-size: 0.95rem;">
            Sur votre téléphone :<br>
            <strong>WhatsApp > Appareils connectés > Connecter un appareil</strong>
          </p>
          <img src="${qrCodeData}" width="280" alt="QR Code WhatsApp" />
          <br><br>
          <button onclick="location.reload()" style="padding: 10px 18px; border-radius: 8px; border: 1px solid #cbd5e1; background: white; cursor: pointer;">
            🔄 Rafraîchir
          </button>
        </div>
      </body>
    </html>
  `);
});

app.post("/send", async (req, res) => {
  try {
    const { to, message } = req.body;

    if (!to || !message) {
      return res.status(400).json({ error: "Les champs 'to' et 'message' sont obligatoires." });
    }

    if (connectionStatus !== "CONNECTED" || !sock) {
      return res.status(530).json({
        error: "Le service WhatsApp n'est pas connecté. Scannez le QR Code sur /qr.",
      });
    }

    let cleanPhone = to.replace(/[^0-9]/g, "");
    if (!cleanPhone.endsWith("@s.whatsapp.net")) {
      cleanPhone = `${cleanPhone}@s.whatsapp.net`;
    }

    await sock.sendMessage(cleanPhone, { text: message });
    console.log(`[WHATSAPP SENT] Message envoyé avec succès à ${cleanPhone}`);

    res.json({ success: true, message: "Message WhatsApp envoyé avec succès." });
  } catch (error) {
    console.error("[WHATSAPP SEND ERROR]", error);
    res.status(500).json({ error: error.message || "Erreur d'envoi WhatsApp." });
  }
});

app.listen(PORT, () => {
  console.log(`[WHATSAPP SERVICE] Serveur à l'écoute sur le port ${PORT}`);
});
