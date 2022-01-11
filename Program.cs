using System;
using System.Collections.Generic;
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
        private static int _patientNumber = 0;

        private static readonly Dictionary<string, string> _fhirServers = new ()
        {
            {"PublicVonk", "http://vonk.fire.ly"},
            {"PublicHapi", "http://hapi.fhir.org/baseR4"},
            {"Local", "http://localhost:8080/fhir"},
        };

        private static FhirClient _fhirClient = new(_fhirServers["PublicVonk"])
        {
            Settings = new FhirClientSettings
            {
                PreferredFormat = ResourceFormat.Json,
                PreferredReturn = Prefer.ReturnRepresentation
            }
        };

        static void Main(string[] args)
        {
            GetPatients(_fhirClient);
        }

        private static IEnumerable<Patient> GetPatients(
            FhirClient client,
            string[] patientCriteria = null, 
            int maxPatients = 20,
            bool onlyWithEncounters = false)
        {
            List<Patient> patients = new();

            Bundle patientBundle;
            if ((patientCriteria == null) || !patientCriteria.Any())
            {
                patientBundle = _fhirClient.Search<Patient>();
            }
            else
            {
                // get list of patients: (really though - get a bundle of resource types)
                // ex: var patientBundle = _fhirClient.Search<Patient>(new []{"name=test"});
                patientBundle = _fhirClient.Search<Patient>(patientCriteria);
            }
            
            // get patients:
            while (patientBundle != null)
            {
                Console.WriteLine($"Patient Bundle.Total: {patientBundle.Total} Entry count: {patientBundle.Entry.Count}");
                
                // look at the patients in the bundle (my code)
                //foreach (var entry in patientBundle.Entry)
                //{
                //    Console.WriteLine($"- Entry {_patientNumber, 3}: {entry.FullUrl}");
                
                //    var patient = entry.Resource as Patient;

                //    if (patient != null)
                //    {
                //        // patient name is a collection. Could .ToString() override
                //        // $"{patient.Name.FirstOrDefault()}" to print the default
                //        // given and family names
                //        Console.WriteLine($" - Id: {patient.Id,20}");
                //        if (patient.Name.Any())
                //        {
                //            Console.WriteLine($"  - Name:  {patient.Name.First()}");
                //        }
                //        PrintEncounters(patient);
                //    }

                //    Console.WriteLine();
                //    _patientNumber++;
                //}

                // gino's code:
                foreach (var entry in patientBundle.Entry)
                {
                    if (entry.Resource != null)
                    {
                        var patient = (Patient) entry.Resource;

                        var encounterBundle = _fhirClient.Search<Encounter>(
                            new []
                            {
                                $"patient=Patient/{patient.Id}",
                            });

                        if (onlyWithEncounters && (encounterBundle.Total == 0))
                        {
                            continue;
                        }

                        patients.Add(patient);

                        Console.WriteLine($"- Entry {patients.Count, 3}: {entry.FullUrl}");
                        Console.WriteLine($" -   Id: {patient.Id,20}");

                        if (patient.Name.Any())
                        {
                            Console.WriteLine($"  - Name:  {patient.Name.First()}");
                        }

                        if (encounterBundle.Total > 0)
                        {
                            Console.WriteLine($" - Encounter total: {encounterBundle.Total}" +
                                          $" Entry count: {encounterBundle.Entry.Count}");
                        }
                        
                    }
                    
                    if (patients.Count >= maxPatients)
                    {
                        break;
                    }
                }
                
                // get more results:
                patientBundle = _fhirClient.Continue(patientBundle);
            }

            return patients;
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
                    // patient name is a collection. Could .ToString() override
                    // $"{patient.Name.FirstOrDefault()}" to print the default
                    // given and family names
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

            Console.WriteLine($" - Encounter total: {encounterBundle.Total}" +
                              $" Entry count: {encounterBundle.Entry.Count}");
        }
    }
}
