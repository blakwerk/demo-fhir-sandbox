using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;

using Task = System.Threading.Tasks.Task;

namespace FhirSample
{
    /// <summary>
    /// Main Program
    /// </summary>
    internal static class Program
    {
        private static readonly Dictionary<string, string> FhirServers = new ()
        {
            {"PublicVonk", "http://vonk.fire.ly"},
            {"PublicHapi", "http://hapi.fhir.org/baseR4"},
            {"Local", "http://localhost:8080/fhir"},
        };

        /// <summary>
        /// Main entry point for the program
        /// </summary>
        /// <param name="args"></param>
        private static async Task<int> Main(string[] args)
        {
            FhirClient fhirClient = new(FhirServers["PublicVonk"])
            {
                Settings = new FhirClientSettings
                {
                    PreferredFormat = ResourceFormat.Json,
                    PreferredReturn = Prefer.ReturnRepresentation
                }
            };

            var patient = await CreatePatientAsync(fhirClient, "Foo", "Bar");
            Console.WriteLine($"Created Patient/{patient.Id}");
            
            var patients = GetPatients(fhirClient, maxPatients: 2, onlyWithEncounters: true);

            Console.WriteLine($"Found {patients.Count()} patients.");

            return 0;
        }

        /// <summary>
        /// Creates a patient with the specified name.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="familyName"></param>
        /// <param name="givenName"></param>
        private static async Task<Patient> CreatePatientAsync(
            FhirClient client,
            string familyName,
            string givenName)
        {
            var patientToCreate = new Patient
            {
                Name = new List<HumanName>
                {
                    new()
                    {
                        Family = familyName,
                        Given = new[] {givenName},
                    },
                },
                BirthDateElement = new Date(1970, 01, 01),
            };

            var created = await client.CreateAsync(patientToCreate);
            return created;
        }

        /// <summary>
        /// Delete a patient, specified by id.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static async Task DeletePatientAsync(FhirClient client, string id)
        {
            //TODO do some better validation on this ID
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(nameof(id));
            }

            // this throws an exception if it fails to delete
            await client.DeleteAsync($"Patient/{id}");
        }

        /// <summary>
        /// Get a collection of patients matching the specified criteria.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="patientCriteria"></param>
        /// <param name="maxPatients">The maximum number of patients returned (default: 20).</param>
        /// <param name="onlyWithEncounters">Flag to only return patients with encounters (default: false).</param>
        /// <returns></returns>
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
                patientBundle = client.Search<Patient>();
            }
            else
            {
                // get list of patients: (really though - get a bundle of resource types)
                // ex: var patientBundle = _fhirClient.Search<Patient>(new []{"name=test"});
                patientBundle = client.Search<Patient>(patientCriteria);
            }
            
            // get patients:
            while (patientBundle != null)
            {
                Console.WriteLine($"Patient Bundle.Total: {patientBundle.Total} Entry count: {patientBundle.Entry.Count}");
                
                // gino's code:
                foreach (var entry in patientBundle.Entry)
                {
                    if (entry.Resource != null)
                    {
                        var patient = (Patient) entry.Resource;

                        var encounterBundle = client.Search<Encounter>(
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
                
                if (patients.Count >= maxPatients)
                {
                    break;
                }

                // get more results:
                patientBundle = client.Continue(patientBundle);
            }

            return patients;
        }
    }
}
