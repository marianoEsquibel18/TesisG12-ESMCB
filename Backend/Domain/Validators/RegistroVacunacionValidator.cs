using Core.Domain.Validators;
using Domain.Entities;
using FluentValidation;

namespace Domain.Validators
{
    public class RegistroVacunacionValidator : EntityValidator<RegistroVacunacion>
    {
        public RegistroVacunacionValidator()
        {
            RuleFor(r => r.PacienteId)
                .NotEmpty().WithMessage("Debe indicar el paciente");

            RuleFor(r => r)
                .Must(r => (r.VacunaId.HasValue && r.VacunaId.Value > 0) || !string.IsNullOrWhiteSpace(r.ProductoId))
                .WithMessage("Debe seleccionar una vacuna válida");

            RuleFor(r => r.FechaAplicacion)
                .NotEmpty().WithMessage("La fecha de aplicación es requerida")
                .LessThanOrEqualTo(DateTime.Now.AddDays(1)).WithMessage("La fecha de aplicación no puede ser futura");

            RuleFor(r => r.Veterinario)
                .NotEmpty().WithMessage("Debe indicar el veterinario que aplicó la vacuna")
                .MaximumLength(100).WithMessage("El nombre del veterinario no puede superar los 100 caracteres");

            RuleFor(r => r.NroLote)
                .MaximumLength(50).WithMessage("El número de lote no puede superar los 50 caracteres");
        }
    }
}
