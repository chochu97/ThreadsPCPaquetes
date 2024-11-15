using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PaquetesThreads
{
    internal class Program // Ejercicio de distribucion de paquetes. Camiones y Empleados.
    {
        private static Queue<string> paqueteQueue = new Queue<string>(); // Creamos la cola/buffer
        private static readonly object lock_ = new object(); // el lock para bloquear
        private const int bufferSize = 8;   // un size max del buffer para no sobresaturarlo
        private static bool isProducing = true; // setteamos un bool para poder identificar cuando no va a haber mas agregacion de paquetes
        // /necesita una condicion de corte basicamente.
        private static Dictionary<string, int> reportes = new Dictionary<string, int>();
        static void Main(string[] args)
        {
            Thread productor1 = new Thread(() => Descargar("Camion A23P"));
            Thread productor2 = new Thread(() => Descargar("Camion WS8J"));

            Thread consumidor1 = new Thread(() => Recoger("Flor Giovannoni"));
            Thread consumidor2 = new Thread(() => Recoger("Cony Perciante"));

            productor1.Start();
            productor2.Start();
            consumidor1.Start();
            consumidor2.Start();

            productor1.Join();
            productor2.Join();

            lock (lock_)
            {
                isProducing = false;
                Monitor.PulseAll(lock_);  // notificamos que termino la descarga a los consumidores
            }

            consumidor1.Join();
            consumidor2.Join();

            Console.WriteLine("\n--- Reporte Final ---");
            foreach(var reporte in reportes)
            {
                Console.WriteLine($"{reporte.Key} proceso {reporte.Value} paquetes.");
            }
            Console.ReadKey();
        }

        static void Descargar(string CamionName)
        {
            for(int i = 1; i <= 20; i++)  // suponemos que los camiones descargan 10 paquetes
            {
                lock (lock_) // el lock arranca primero
                {
                    while (paqueteQueue.Count >= bufferSize)
                    {
                        Monitor.Wait(lock_);
                    }

                    string paquete = $"{CamionName} - Paquete N°{i}";  // creamos el paquete
                    paqueteQueue.Enqueue(paquete);  // lo descargamos y agregamos a la cola
                    Console.WriteLine($"Descarga finalizada: {paquete}");

                    Monitor.Pulse(lock_);  // Notificamos a un consumidor que hay un nuevo item
 
                }
                Thread.Sleep(new Random().Next(200, 500)); // simulamos tiempo de descarga
               
            }
        }

        static void Recoger(string EmpleadoName)
        {
            while (true)
            {
                string paquete;
                lock (lock_)
                {
                    while(paqueteQueue.Count == 0)
                    {
                        if (!isProducing)
                        {
                            return;
                        }

                        Monitor.Wait(lock_);
                    }

                    paquete = paqueteQueue.Dequeue();
                    Console.WriteLine($"El Empleado {EmpleadoName} ha recolectado: {paquete}");

                    if (!reportes.ContainsKey(EmpleadoName))
                    {
                        reportes[EmpleadoName] = 0;
                    }
                    reportes[EmpleadoName]++;

                    Monitor.Pulse(lock_); // Notificamos a los productores que se libero un espacio
                }
                Thread.Sleep(new Random().Next(150, 700));  // simulamos un tiempo de recoleccion
            }
        }
    }
}
