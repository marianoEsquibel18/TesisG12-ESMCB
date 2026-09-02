using Core.Domain.Validators;
using Domain.Entities;
using FluentValidation;

namespace Domain.Validators
{
    public class TurnoValidator : EntityValidator<Turno>
    {
        public TurnoValidator()
        {
            RuleFor(t => t.PacienteId)
                .NotEmpty().WithMessage("Debe indicar el paciente");

            RuleFor(t => t.VeterinarioId)
                .NotEmpty().WithMessage("Debe indicar el veterinario");

            RuleFor(t => t.ServicioId)
                .GreaterThan(0).WithMessage("Debe seleccionar un servicio válido");

            RuleFor(t => t.FechaHora)
                .NotEmpty().WithMessage("La fecha y hora del turno es requerida")
                .GreaterThanOrEqualTo(DateTime.Now.AddMinutes(29)).WithMessage("Los turnos deben agendarse con al menos 30 minutos de anticipación");

            RuleFor(t => t.DuracionMinutos)
                .GreaterThan(0).WithMessage("La duración debe ser mayor a 0 minutos")
                .LessThanOrEqualTo(480).WithMessage("La duración no puede superar las 8 horas");

            RuleFor(t => t.Motivo)
                .MaximumLength(200).WithMessage("El motivo no puede superar los 200 caracteres");

            RuleFor(t => t.Observaciones)
                .MaximumLength(500).WithMessage("Las observaciones no pueden superar los 500 caracteres");
        }
    }
}
