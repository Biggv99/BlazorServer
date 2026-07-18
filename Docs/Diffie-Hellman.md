# Diffie-Hellman: Architettura Scambio Chiavi ECDH + EKE (Blob Crittografica)

Questo documento descrive un protocollo agnostico di scambio chiavi tra un Client e un Server. Il protocollo utilizza **ECDH** (Elliptic Curve Diffie-Hellman) per la Forward Secrecy e **EKE** (Encrypted Key Exchange) per autenticare lo scambio tramite una password pre-condivisa.

## 🔑 I Tre Segreti Pre-Condivisi
Prima di iniziare il flusso, il sistema assume che Client e Server condividano in modo sicuro le seguenti informazioni:

1. **Password EKE:** Usata come entropia iniziale per derivare una chiave simmetrica (tramite PBKDF2).
2. **Server Key Master:** Una chiave AES statica di livello superiore, conosciuta da entrambi, usata per sigillare l'intero pacchetto finale (Involucro Esterno).
3. **Credenziali Utente (Es. Email/Password):** Utilizzate per la standard autenticazione (AuthN) del payload iniziale.

---

## 🔄 Il Flusso del Protocollo

### Fase 1: Richiesta del Client
Il Client avvia la registrazione senza trasmettere materiale crittografico in chiaro.

1. **Generazione Effimera:** Il Client genera una coppia di chiavi effimere ("usa e getta") per l'ECDH.
2. **Cifratura (EKE):** Il Client cifra la sua **Chiave Pubblica Effimera** in `AES-GCM` derivando la chiave di cifratura dalla *Password EKE* condivisa.
3. **Trasmissione:** Il Client invia al Server un payload contenente: le credenziali utente e il pacchetto crittografico (Ciphertext, Nonce, Tag, Salt).

### Fase 2: Elaborazione del Server
Il Server riceve la richiesta e orchestra la creazione della chiave definitiva del dispositivo.

1. **Decifratura Client:** Il Server deriva la chiave dalla *Password EKE* (usando il Salt ricevuto) e decifra la **Chiave Pubblica Effimera del Client**.
2. **Generazione Effimera Server:** Il Server genera la propria **Coppia di Chiavi Effimere** (ECDH).
3. **Calcolo Segreto Condiviso:** Il Server calcola il **Segreto Condiviso** combinando la sua *Chiave Privata Effimera* con la *Chiave Pubblica Effimera del Client*. Il risultato viene espanso (es. tramite HKDF) in una chiave robusta (es. 32 byte).
4. **Generazione Chiave Definitiva:** Il Server genera una **TERZA** coppia di chiavi (ECDSA). Questa sarà l'identità definitiva del dispositivo. Il Server salva la *Chiave Pubblica Definitiva* nel proprio database e prepara la *Chiave Privata Definitiva* per la spedizione al Client.

### Fase 3: La "Matrioska" Crittografica (Risposta del Server)
Il Server costruisce un payload blindato per consegnare la chiave definitiva in modo sicuro, strutturato in tre strati:

* **Involucro Interno 1 (Chiave Pubblica Server):** Cifra la *Chiave Pubblica Effimera del Server* usando la *Password EKE*. (Produce: `Cipher1, Nonce1, Tag1, Salt1`).
* **Involucro Interno 2 (Chiave Privata Definitiva):** Cifra la *Chiave Privata Definitiva* applicando un doppio strato:
  1. Cifratura con la *Password EKE*.
  2. Sovra-cifratura con il *Segreto Condiviso* (calcolato in Fase 2).
  (Produce: `Cipher2, Nonce2, Tag2, Salt2`).
* **Assemblaggio:** Concatena i parametri con un separatore (es. `:`):
  `Cipher1:Nonce1:Tag1:Salt1:Cipher2:Nonce2:Tag2:Salt2`
* **Involucro Esterno:** Cifra l'intera stringa concatenata usando l'AES-GCM con la **Server Key Master**. Il blob Base64 risultante viene inviato al Client.

### Fase 4: Spacchettamento del Client
Il Client riceve il blob Base64 e inverte il processo per estrarre la sua identità.

1. **Apertura Involucro Esterno:** Decifra il payload complessivo utilizzando la **Server Key Master**.
2. **Recupero Chiave Server:** Estrae i primi 4 parametri e decifra (usando la *Password EKE*) la **Chiave Pubblica Effimera del Server**.
3. **Calcolo Segreto Condiviso:** Combina la propria *Chiave Privata Effimera* (della Fase 1) con la *Chiave Pubblica Effimera del Server* appena estratta. Ora il Client e il Server possiedono lo stesso **Segreto Condiviso**.
4. **Estrazione Chiave Definitiva:** Utilizza il *Segreto Condiviso* e la *Password EKE* per rimuovere la doppia cifratura dagli ultimi 4 parametri del blob.
5. **Salvataggio:** Il Client ottiene la **Chiave Privata Definitiva** in chiaro e la archivia nel proprio Secure Storage (es. KeyStore/Keychain).

> **Risultato Finale:** Il dispositivo possiede ora una chiave privata unica e crittograficamente sicura per firmare (ECDSA) le future richieste API, completando il binding crittografico (Device Provisioning).