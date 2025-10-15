using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Features;

namespace Zapto.AspNetCore.Http.Features;

internal class FeatureCollectionImpl : IFeatureCollection
{
    private FeatureCollection? _features = new();
    private readonly HttpResponseBodyFeatureImpl _defaultResponseBodyFeature;
    private readonly HttpsCompressionFeatureImpl _defaultHttpsCompressionFeature;

    private int _revision;
    private IHttpResponseBodyFeature? _responseBodyFeature;
    private IHttpsCompressionFeature? _httpsCompressionFeature;

    public FeatureCollectionImpl()
    {
        _defaultResponseBodyFeature = new HttpResponseBodyFeatureImpl();
        _responseBodyFeature = _defaultResponseBodyFeature;

        _defaultHttpsCompressionFeature = new HttpsCompressionFeatureImpl();
        _httpsCompressionFeature = _defaultHttpsCompressionFeature;
    }

    private FeatureCollection Features => _features ??= new FeatureCollection();

    public void SetHttpResponse(AspNetResponse response)
    {
        _defaultResponseBodyFeature.SetHttpResponse(response);
        _defaultHttpsCompressionFeature.SetHttpResponse(response);
    }

    public void Reset()
    {
        _features = null;
        _revision = 0;

        _responseBodyFeature = _defaultResponseBodyFeature;
        _defaultResponseBodyFeature.Reset();

        _httpsCompressionFeature = _defaultHttpsCompressionFeature;
        _defaultHttpsCompressionFeature.Reset();
    }

    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => Features.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Features).GetEnumerator();

    public TFeature Get<TFeature>() => (TFeature) this[typeof (TFeature)];

    public void Set<TFeature>(TFeature instance) => this[typeof (TFeature)] = instance!;

    public bool IsReadOnly => Features.IsReadOnly;

    public int Revision => Features.Revision + _revision;

    public object? this[Type key]
    {
        get
        {
            if (key == typeof(IHttpResponseBodyFeature))
            {
                return _responseBodyFeature;
            }

            if (key == typeof(IHttpsCompressionFeature))
            {
                return _httpsCompressionFeature;
            }

            return Features[key];
        }
        set
        {
            if (key == typeof(IHttpResponseBodyFeature))
            {
                _responseBodyFeature = (IHttpResponseBodyFeature?) value;
                _revision++;
                return;
            }

            if (key == typeof(IHttpsCompressionFeature))
            {
                _httpsCompressionFeature = (IHttpsCompressionFeature?) value;
                _revision++;
                return;
            }

            Features[key] = value;
        }
    }
}
