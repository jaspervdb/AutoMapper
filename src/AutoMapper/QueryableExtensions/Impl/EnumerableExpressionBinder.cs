using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Concurrent;

namespace AutoMapper.QueryableExtensions.Impl
{
    using Configuration;
    using Mappers;

    public class EnumerableExpressionBinder : IExpressionBinder
    {
        public bool IsMatch(PropertyMap propertyMap, TypeMap propertyTypeMap, ExpressionResolutionResult result)
        {
            return propertyMap.DestinationPropertyType.IsEnumerableType() && !TypeHelper.GetElementType(propertyMap.DestinationPropertyType).IsPrimitive();
        }

        public MemberAssignment Build(IConfigurationProvider configuration, PropertyMap propertyMap, TypeMap propertyTypeMap, ExpressionRequest request, ExpressionResolutionResult result, ConcurrentDictionary<ExpressionRequest, int> typePairCount)
        {
            return BindEnumerableExpression(configuration, propertyMap, request, result, typePairCount);
        }

        private static MemberAssignment BindEnumerableExpression(IConfigurationProvider configuration, PropertyMap propertyMap, ExpressionRequest request, ExpressionResolutionResult result, ConcurrentDictionary<ExpressionRequest, int> typePairCount)
        {
            var destinationListType = TypeHelper.GetElementType(propertyMap.DestinationPropertyType);
            var sourceListType = TypeHelper.GetElementType(propertyMap.SourceType);
            var expression = result.ResolutionExpression;

            if (sourceListType != destinationListType)
            {
                var listTypePair = new ExpressionRequest(sourceListType, destinationListType, request.MembersToExpand);
                var transformedExpression = configuration.ExpressionBuilder.CreateMapExpression(listTypePair, typePairCount);
                if(transformedExpression == null)
                {
                    return null;
                }
                expression = Expression.Call(typeof (Enumerable), "Select", new[] {sourceListType, destinationListType}, result.ResolutionExpression, transformedExpression);
            }

            expression = Expression.Call(typeof(Enumerable), propertyMap.DestinationPropertyType.IsArray ? "ToArray" : "ToList", new[] { destinationListType }, expression);

            if(configuration.Configuration.AllowNullCollections) {
                expression = Expression.Condition(
                            Expression.NotEqual(
                                Expression.TypeAs(result.ResolutionExpression, typeof(object)), 
                                Expression.Constant(null)),
                            expression,
                            Expression.Constant(null, propertyMap.DestinationPropertyType));
            }

            return Expression.Bind(propertyMap.DestinationProperty, expression);
        }
    }
}