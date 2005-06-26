// 
// lat - CertMgr.cs
//
// Contains code from the Mono project written by 
// Sebastien Pouliot <sebastien@ximian.com> and 
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
// source file: mcs/tools/security/certmgr.cs
//
// Minor modifications to integrate into LAT written by
// Loren Bandiera <lorenb@mmgsecurity.com>
//
//

using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using SSCX = System.Security.Cryptography.X509Certificates;
using System.Text;

using Mono.Security.Authenticode;
using Mono.Security.Cryptography;
using Mono.Security.X509;
using Mono.Security.Protocol.Tls;

namespace lat 
{
	class CertificateManager
	{
		CertificateManager () {}

//		private static Mono.Security.Protocol.Tls.SecurityProtocolType protocol = Mono.Security.Protocol.Tls.SecurityProtocolType.Default;
//		private static X509CertificateCollection certificates = new X509CertificateCollection ();

	
		static X509Store GetStoreFromName (string storeName, bool machine) 
		{
			X509Stores stores = ((machine) ? X509StoreManager.LocalMachine : X509StoreManager.CurrentUser);
			X509Store store = null;
			switch (storeName) {
				case X509Stores.Names.Personal:
					return stores.Personal;
				case X509Stores.Names.OtherPeople:
					return stores.OtherPeople;
				case X509Stores.Names.IntermediateCA:
					return stores.IntermediateCA;
				case X509Stores.Names.TrustedRoot:
					return stores.TrustedRoot;
				case X509Stores.Names.Untrusted:
					return stores.Untrusted;
			}
			return store;
		}

		static bool CertificateValidation (SSCX.X509Certificate certificate, int[] certificateErrors)
		{
			// the main reason to download it is that it's not trusted
			return true;
			// OTOH we ask user confirmation before adding certificates into the stores
		}
		
		static X509CertificateCollection GetCertificatesFromSslSession (string url) 
		{
			Uri uri = new Uri (url);
			IPHostEntry host = Dns.Resolve (uri.Host);
			IPAddress ip = host.AddressList [0];
			Socket socket = new Socket (ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect (new IPEndPoint (ip, uri.Port));
			NetworkStream ns = new NetworkStream (socket, false);
			SslClientStream ssl = new SslClientStream (ns, uri.Host, false, Mono.Security.Protocol.Tls.SecurityProtocolType.Default, null);
			ssl.ServerCertValidationDelegate += new CertificateValidationCallback (CertificateValidation);

			try {
				// we don't really want to write to the server (as we don't know
				// the protocol it using) but we must send something to be sure the
				// SSL handshake is done (so we receive the X.509 certificates).
				StreamWriter sw = new StreamWriter (ssl);
				sw.WriteLine (Environment.NewLine);
				sw.Flush ();
				socket.Poll (30000, SelectMode.SelectRead);
			}
			finally {
				socket.Close ();
			}

			// we need a little reflection magic to get this information
			PropertyInfo pi = typeof (SslClientStream).GetProperty ("ServerCertificates", BindingFlags.Instance | BindingFlags.NonPublic);
			if (pi == null) {
				Console.WriteLine ("Sorry but you need a newer version of Mono.Security.dll to use this feature.");
				return null;
			}
			return (X509CertificateCollection) pi.GetValue (ssl, null);
		}

		public static bool Ssl (string host, Gtk.Window parent)
		{
			Logger.Log.Debug ("Importing certificates from '{0}' into the user's store.", host);

			bool retVal = true;

			X509CertificateCollection coll = GetCertificatesFromSslSession (host);

			if (coll != null) {
				X509Store store = null;
				// start by the end (root) so we can stop adding them anytime afterward
				for (int i = coll.Count - 1; i >= 0; i--) {
					X509Certificate x509 = coll [i];
					bool selfsign = false;
					bool failed = false;
					try {
						selfsign = x509.IsSelfSigned;
					}
					catch {
						// sadly it's hard to interpret old certificates with MD2
						// without manually changing the machine.config file
						failed = true;
					}

					if (selfsign) {
						// this is a root
						store = GetStoreFromName (X509Stores.Names.TrustedRoot, false);
					} else if (i == 0) {
						// server certificate isn't (generally) an intermediate CA
						store = GetStoreFromName (X509Stores.Names.OtherPeople, false);
					} else {
						// all other certificates should be intermediate CA
						store = GetStoreFromName (X509Stores.Names.IntermediateCA, false);
					}

					if (store.Certificates.Contains (x509)) {
						Logger.Log.Debug ("Store already contains certificate");
						break;
					}

					string msg = String.Format ("{0}{1} X.509 Certificate v{2}", 	
						Environment.NewLine,
						selfsign ? "Self-signed " : String.Empty,
						x509.Version);
	
					msg += String.Format ("Issued from: {0}\n", x509.IssuerName);
					msg += String.Format ("Issued to:   {0}\n", x509.SubjectName);
					msg += String.Format ("Valid from:  {0}\n", x509.ValidFrom);
					msg += String.Format ("Valid until: {0}\n", x509.ValidUntil);

					if (!x509.IsCurrent)
						msg += "\n   *** WARNING: Certificate isn't current ***\n";
					if ((i > 0) && !selfsign) {
						X509Certificate signer = coll [i-1];
						bool signed = false;
						try {
							if (signer.RSA != null) {
								signed = x509.VerifySignature (signer.RSA);
							} else if (signer.DSA != null) {
								signed = x509.VerifySignature (signer.RSA);
							} else {
								msg += "\n   *** WARNING: Couldn't not find who signed this certificate ***\n";
								signed = true; // skip next warning
							}

							if (!signed)
								msg += "\n   *** WARNING: Certificate signature is INVALID ***\n";
						}
						catch {
							failed = true;
						}
					}
					if (failed) {

						Gtk.MessageDialog ed = new Gtk.MessageDialog (parent, 
							Gtk.DialogFlags.DestroyWithParent,
							Gtk.MessageType.Error, 
							Gtk.ButtonsType.Close, 
							"Couldn't decode certificate properly");

						ed.Run ();
						ed.Destroy();
		
						retVal = false;

						break;
					}

					CertificateDialog cd = new CertificateDialog (msg);

					Logger.Log.Debug ("CertificateDialog.UserResponse: {0}", cd.UserResponse);

					if (cd.UserResponse == CertDialogResponse.Import) 
					{
						store.Import (x509);
						Logger.Log.Debug ("Certificate successfully imported.");
					} 
					else if (cd.UserResponse == CertDialogResponse.Cancel)
					{
						retVal = false;
					}
				}			
			}

			return retVal;
		}
	}
}
