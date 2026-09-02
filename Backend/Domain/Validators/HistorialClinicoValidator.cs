using Core.Domain.Validators;
using Domain.Entities;
using FluentValidation;

namespace Domain.Validators
{
    public class HistorialClinicoValidator : EntityValidator<HistorialClinico>
    {
        public HistorialClinicoValidator()
        {
            RuleFor(h => h.PacienteId)
                .NotEmpty().WithMessage("Debe indicar el paciente");

            RuleFor(h => h.Fecha)
                .NotEmpty().WithMessage("La fecha de la consulta es requerida")
                .LessThanOrEqualTo(DateTime.Now).WithMessage("La fecha no puede ser futura");

            RuleFor(h => h.Motivo)
                .NotEmpty().WithMessage("El motivo de consulta es requerido")
                .MaximumLength(200).WithMessage("El motivo no puede superar los 200 caracteres");

            RuleFor(h => h.Veterinario)
                .NotEmpty().WithMessage("Debe indicar el veterinario")
                .MaximumLength(100).WithMessage("El nombre del veterinario no puede superar los 100 caracteres");

            RuleFor(h => h.Sintomas)
                .MaximumLength(1000).WithMessage("Los síntomas no pueden superar los 1000 caracteres");

            RuleFor(h => h.Diagnostico)
                .MaximumLength(500).WithMessage("El diagnóstico no puede superar los 500 caracteres");

            RuleFor(h => h.Indicaciones)
                .MaximumLength(1000).WithMessage("Las indicaciones no pueden superar los 1000 caracteres");

            RuleFor(h => h.Peso)
                .GreaterThan(0).When(h => h.Peso.HasValue)
                .WithMessage("El peso debe ser mayor a 0");

            RuleFor(h => h.Temperatura)
                .InclusiveBetween(1, 50).When(h => h.Temperatura.HasValue)
                .WithMessage("La temperatura debe estar entre 1 y 50 grados");
        }
    }
}
