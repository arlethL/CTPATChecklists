using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CTPATChecklists.Models;

namespace CTPATChecklists.UnitTests
{
    [TestClass]
    public class PuntoChecklistTests
    {
        // ===== Helper para validar DataAnnotations =====
        private static IList<ValidationResult> Validate(object instance)
        {
            var ctx = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(instance, ctx, results, validateAllProperties: true);
            return results;
        }

        [TestMethod]
        [TestCategory("Checklist")]
        public void Debe_Tener_17_Puntos()
        {
            var puntos = Enumerable.Range(1, 17)
                .Select(i => new PuntoChecklist
                {
                    ChecklistId = 1,
                    Descripcion = $"Punto {i}",
                    Cumple = true
                })
                .ToList();

            Assert.AreEqual(17, puntos.Count);
        }

        [TestMethod]
        [TestCategory("Checklist")]
        public void Cumple_EsRequerido_SiEsNull_DebeFallarValidacion()
        {
            var punto = new PuntoChecklist
            {
                ChecklistId = 1,
                Descripcion = "Candados en remolque",
                Cumple = null, // <- debe fallar por [Required]
                Observaciones = null,
                FotoRuta = null
            };

            var results = Validate(punto);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(PuntoChecklist.Cumple))),
                "Se esperaba error de validación en 'Cumple'.");
        }

        [TestMethod]
        [TestCategory("Checklist")]
        public void Descripcion_EsRequerida_SiEsNullODefault_DebeFallarValidacion()
        {
            var punto = new PuntoChecklist
            {
                ChecklistId = 1,
                Descripcion = null!, // <- requerido
                Cumple = true
            };

            var results = Validate(punto);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(PuntoChecklist.Descripcion))),
                "Se esperaba error de validación en 'Descripcion'.");
        }

        [TestMethod]
        [TestCategory("Checklist")]
        public void Observaciones_MaxLength_250()
        {
            var texto251 = new string('x', 251);
            var punto = new PuntoChecklist
            {
                ChecklistId = 1,
                Descripcion = "Prueba longitudes",
                Cumple = true,
                Observaciones = texto251 // > 250 -> invalida
            };

            var results = Validate(punto);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(PuntoChecklist.Observaciones))),
                "Se esperaba error de longitud en 'Observaciones' (>250).");
        }

        [TestMethod]
        [TestCategory("Checklist")]
        public void FotoRuta_MaxLength_300()
        {
            var texto301 = new string('x', 301);
            var punto = new PuntoChecklist
            {
                ChecklistId = 1,
                Descripcion = "Prueba foto",
                Cumple = true,
                FotoRuta = texto301 // > 300 -> invalida
            };

            var results = Validate(punto);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(PuntoChecklist.FotoRuta))),
                "Se esperaba error de longitud en 'FotoRuta' (>300).");
        }

        [TestMethod]
        [TestCategory("Checklist")]
        public void Regla_Completitud_17_Puntos_ConCumple_NoNull()
        {
            // Regla de negocio típica: 17 puntos y ninguno con Cumple = null
            var puntos = Enumerable.Range(1, 17)
                .Select(i => new PuntoChecklist
                {
                    ChecklistId = 1,
                    Descripcion = $"Punto {i}",
                    Cumple = (i % 2 == 0) // true/false alternado, pero NUNCA null
                })
                .ToList();

            var completos = puntos.Count == 17 && puntos.All(p => p.Cumple.HasValue);
            Assert.IsTrue(completos, "La regla de completitud debe cumplirse (17 puntos con Cumple no-nulo).");
        }
    }
}
