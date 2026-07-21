using MediatR;

namespace BuildingBlocks.CQRS;

/* ESTA INTERFAZ DEVUELCE UN RESULTADO DE CONSULTA NOT NULL*/

public interface IQuery<out TResponse>:IRequest<TResponse>
    where TResponse : notnull
{
}
