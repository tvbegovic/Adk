angular.module('app').
    factory('priceDesigner11Service', function () {
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
        }        
        
        return factory;
    });
