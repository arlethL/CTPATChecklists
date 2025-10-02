using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CTPATChecklists.Models
{
    public class Checklist
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El campo Placa es obligatorio")]
        [StringLength(20)]
        public string? Placa { get; set; }

        [Required(ErrorMessage = "El campo Operador es obligatorio")]
        [StringLength(100)]
        public string? Operador { get; set; }

        [Required(ErrorMessage = "El campo Empresa es obligatorio")]
        [StringLength(100)]
        public string? Empresa { get; set; }

        [Required(ErrorMessage = "Debes seleccionar Entrada o Salida")]
        public bool? EsEntrada { get; set; }

        public DateTime FechaHora { get; set; }

        public List<PuntoChecklist> Puntos { get; set; } = new();
        public List<FotoChecklist> Fotos { get; set; } = new();

        public string? UsuarioId { get; set; }
        public ApplicationUser? Usuario { get; set; }

        [Required(ErrorMessage = "El campo Caja No. es obligatorio")]
        [Display(Name = "ID de la caja del tráiler/camión")]
        [StringLength(50)]
        public string? IdCajaTrailer { get; set; }

        [Display(Name = "¿Incluir foto de cámara IP?")]
        public bool IncluirFotoCamara { get; set; }

        public string? FotoDesdeCamaraRuta { get; set; }

        // ---------------------------------------------
        // CAMPOS CTPAT OBLIGATORIOS
        // ---------------------------------------------

        [Required(ErrorMessage = "El folio de hoja viajera es obligatorio")]
        public string? FolioHojaViajera { get; set; }

        [Required(ErrorMessage = "El folio de inspección origen es obligatorio")]
        public string? FolioInspeccionOrigen { get; set; }

        [Required(ErrorMessage = "El folio de la inspección es obligatorio")]
        public string? FolioInspeccion { get; set; }

        [Required(ErrorMessage = "El campo Línea es obligatorio")]
        public string? Linea { get; set; }

        [Required(ErrorMessage = "El campo Estado es obligatorio")]
        public string? Estado { get; set; }

        [Required(ErrorMessage = "La sucursal de origen es obligatoria")]
        public string? SucursalOrigen { get; set; }

        [Required(ErrorMessage = "La sucursal destino es obligatoria")]
        public string? SucursalDestino { get; set; }

        [Required(ErrorMessage = "La fecha/hora de salida es obligatoria")]
        [Display(Name = "Fecha/Hora Salida")]
        public DateTime? FechaHoraSalida { get; set; }

        [Required(ErrorMessage = "La fecha/hora de entrada es obligatoria")]
        [Display(Name = "Fecha/Hora Entrada")]
        public DateTime? FechaHoraEntrada { get; set; }

        [Required(ErrorMessage = "La hora de inicio de inspección es obligatoria")]
        public DateTime? HoraInicioInspeccion { get; set; }

        [Required(ErrorMessage = "La hora final de inspección es obligatoria")]
        public DateTime? HoraFinalInspeccion { get; set; }

        [Required(ErrorMessage = "El campo Fianza es obligatorio")]
        public string? Fianza { get; set; }

        [Required(ErrorMessage = "El campo Tractor es obligatorio")]
        public string? MarcaTractor { get; set; }

        // Datos del Remolque
        [Required(ErrorMessage = "El año del remolque es obligatorio")]
        public int? RemolqueAnio { get; set; }

        [Required(ErrorMessage = "El VIN del remolque es obligatorio")]
        [Display(Name = "VIN del Remolque")]
        public string? RemolqueVIN { get; set; }

        [Required(ErrorMessage = "La marca del remolque es obligatoria")]
        public string? RemolqueMarca { get; set; }

        // Datos del Contenedor
        [Required(ErrorMessage = "El año del contenedor es obligatorio")]
        public int? ContenedorAnio { get; set; }

        [Required(ErrorMessage = "El VIN del contenedor es obligatorio")]
        [Display(Name = "VIN del Contenedor")]
        public string? ContenedorVIN { get; set; }

        [Required(ErrorMessage = "La marca del contenedor es obligatoria")]
        public string? ContenedorMarca { get; set; }

        [Required(ErrorMessage = "El número de sello es obligatorio")]
        [Display(Name = "N° de Sello Cargada/Vacía")]
        public string? SelloCarga { get; set; }

        [Required(ErrorMessage = "El sello adicional es obligatorio")]
        [Display(Name = "Sello adicional colocado")]
        public string? SelloAdicional { get; set; }

        [Required(ErrorMessage = "El campo Hora/Lugar/Motivo es obligatorio")]
        [Display(Name = "Hora/Lugar/Motivo")]
        public string? HoraLugarMotivoRetiroSello { get; set; }

        

        [Required(ErrorMessage = "La firma del Operador de Origen es obligatoria.")]
        public string FirmaOperadorOrigen { get; set; }

        [Required(ErrorMessage = "La firma del Oficial es obligatoria.")]
        public string FirmaOficial { get; set; }

        public string? FirmaSupervisor { get; set; }

    }
}
