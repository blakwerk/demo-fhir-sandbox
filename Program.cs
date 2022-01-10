using System;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;

namespace FhirSample
{
    /// <summary>
    /// Main Program
    /// </summary>
    internal static class Program
    {
        // vonk is an open r4 fhir server
        private const string _fhirServer = "http://vonk.fire.ly";
        
        // hapi is another open test server we could play with
        //private const string _fhirServer = "http://hapi.fhir.org/baseR4";

        private static int _patientNumber = 0;

        private static FhirClient _fhirClient = new(_fhirServer)
        {
            Settings = new FhirClientSettings
            {
                PreferredFormat = ResourceFormat.Json,
                PreferredReturn = Prefer.ReturnRepresentation
            }
        };

        static void Main(string[] args)
        {
            //var fhirClient = new FhirClient(_fhirServer)
            //{
            //    Settings = new FhirClientSettings
            //    {
            //        PreferredFormat = ResourceFormat.Json,
            //        PreferredReturn = Prefer.ReturnRepresentation
            //    }
            //};

            // get list of patients: (really though - get a bundle of resource types)
            var patientBundle = _fhirClient.Search<Patient>(new []{"name=test"});

            Console.WriteLine(patientBundle == null
                ? "Total: <bundle was empty>"
                : $"Total: {patientBundle.Total}");
            
            // get all patients:
            do
            {
                Console.WriteLine($"Total: {patientBundle.Total} Entry count: {patientBundle.Entry.Count}");
                PrintPatients(patientBundle);

                // get more results:
                patientBundle = _fhirClient.Continue(patientBundle);
                //if (patientBundle == null)
                //{
                //    break;
                //}

            } while (patientBundle != null);
        }

        private static void PrintPatients(Bundle patientBundle)
        {
            // look at the patients in the bundle
            foreach (var entry in patientBundle.Entry)
            {
                Console.WriteLine($"- Entry {_patientNumber, 3}: {entry.FullUrl}");
                
                var patient = entry.Resource as Patient;

                if (patient != null)
                {
                    // {patient.Name.FirstOrDefault()}
                    Console.WriteLine($" - Id: {patient.Id,20}");
                    if (patient.Name.Any())
                    {
                        Console.WriteLine($"  - Name:  {patient.Name.First()}");
                    }
                    PrintEncounters(patient);
                }

                Console.WriteLine();
                _patientNumber++;
            }
        }

        private static void PrintEncounters(Patient patient)
        {
            var encounterBundle = _fhirClient.Search<Encounter>(
                new[] {$"patient=Patient/{patient.Id}"});

            Console.WriteLine($" - Encounter total: {encounterBundle.Total} Entry count: {encounterBundle.Entry.Count}");
        }
    }
}
