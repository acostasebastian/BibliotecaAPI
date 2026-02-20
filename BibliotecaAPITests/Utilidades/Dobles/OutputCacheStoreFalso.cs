using Microsoft.AspNetCore.OutputCaching;
using System;
using System.Collections.Generic;
using System.Text;

namespace BibliotecaAPITests.Utilidades.Dobles
{
    //Esta clase se usa para simular el cache que se necesita a la hora de crear un autor (  await ouputCacheStore.EvictByTagAsync(cache, default); )
    public class OutputCacheStoreFalso : IOutputCacheStore
    {
        public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
        {
            //solo devuelve que la acción fue completada, como si fuera un OK
            return ValueTask.CompletedTask;
        }

        public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
