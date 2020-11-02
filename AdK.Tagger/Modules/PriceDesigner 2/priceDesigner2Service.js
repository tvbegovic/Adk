angular.module('app').
    factory('priceDesigner2Service', function () {
        var factory = {};
        factory.Id = '';        
    
        factory.init = function () {
            factory.channel = {};
            factory.Id = '';
            factory.pendingSave = false;            
            factory.selectedDayPart = null;
            factory.AllProducts = [];
			factory.dayPartsUsageCount = {};

            factory.removedDayPartsIds = [];
			factory.listPriceDefs = [];
			factory.dictPriceDefinitions = {};
        }        
        
        return factory;
    });
