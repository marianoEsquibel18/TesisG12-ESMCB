using Core.Domain.Validators;
using Domain.Entities;
using FluentValidation;

namespace Domain.Validators
{
    public class ProductoValidator : EntityValidator<Producto>
    {
        public ProductoValidator()
        {
            RuleFor(p => p.Nombre)
                .NotEmpty().WithMessage("El nombre del producto es requerido")
                .MaximumLength(150).WithMessage("El nombre no puede superar los 150 caracteres");
            RuleFor(p => p.CategoriaId)
                .GreaterThan(0).WithMessage("Debe seleccionar una categoría");
            RuleFor(p => p.PrecioCompra)
                .GreaterThanOrEqualTo(0).WithMessage("El precio de compra no puede ser negativo");
            RuleFor(p => p.PrecioVenta)
                .GreaterThanOrEqualTo(0).WithMessage("El precio de venta no puede ser negativo")
                .GreaterThanOrEqualTo(p => p.PrecioCompra).WithMessage("El precio de venta no puede ser menor al precio de compra");
            RuleFor(p => p.StockMinimo)
                .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo no puede ser negativo");
            RuleFor(p => p.CodigoBarras)
                .MaximumLength(50).WithMessage("El código de barras no puede superar los 50 caracteres");
        }
    }
}
