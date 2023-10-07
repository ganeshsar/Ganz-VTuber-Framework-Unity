using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

public class PipeServer : MonoBehaviour
{
    private NamedPipeServerStream server;

    /// <summary>
    /// Called each time Unity recieves data about the face (on a different thread!)
    /// </summary>
    /// <param name="data">The data first single face. Up to the reciever to parse it.</param>
    public delegate void OnDetection(string[] data);
    public event OnDetection onDetection;

    private void Start()
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        Thread t = new Thread(new ThreadStart(Run));
        t.Start();

    }

    private string[] lines= new string[0];
    private void Run()
    {
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        // Open the named pipe.
        server = new NamedPipeServerStream("UnityMediaPipeFace",PipeDirection.InOut, 99, PipeTransmissionMode.Message);

        print("Waiting for connection...");
        server.WaitForConnection();

        print("Connected.");
        var br = new BinaryReader(server, Encoding.UTF8);

        while (true)
        {
            try
            {
                var len = (int)br.ReadUInt32();
                var str = new string(br.ReadChars(len));

                lines = str.Split('\n');
                onDetection?.Invoke(lines);
            }
            catch (EndOfStreamException)
            {
                OnDisable();
                Run();
                break;                    // When client disconnects
            }
        }

    }

    private void OnDisable()
    {
        print("Client disconnected.");
        server.Close();
        server.Dispose();
    }
}
