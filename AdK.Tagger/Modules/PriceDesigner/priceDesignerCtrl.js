angular.module('app')
    .controller('priceDesignerCtrl', ['$scope', '$filter', '$modal', 'confirmPopup', 'Service', function ($scope, $filter, $modal, confirmPopup, Service) {

        $scope.isEmpty = function (o) { return _.isEmpty(o); }

        var initPageLoad = _.once(init);

        function init() {

            $scope.channelFilter = '';
            $scope.productFilter = '';

            $scope.selectedChannel = {};
            $scope.selectedProduct = {};
            $scope.selectedChannelProduct = {};

            //$scope.newProductInputVisible = false;

            //$scope.newProductName = '';

            $scope.pendingSave = false;

            $scope.spotPrice = 0.0;
            $scope.spotDuration = 0;

            $scope.firstCell = {};

            loadChannels();
            loadProducts();

        }

        /*****Channels******/
        function loadChannels() {
            Service('GetChannels').then(function (Channels) {
                $scope.Channels = Channels;

                $scope.selectedChannel = $scope.Channels[0];
                $scope.channelClicked($scope.selectedChannel);
            });
        }

        $scope.channelClicked = function (channel) {

            if ($scope.pendingSave)
                confirmPopup.open('Discard changes?', null, 'Discard all changes and switch to another channel?').then(function () {
                    switchChannel(channel);
                });
            else
                switchChannel(channel);
        }

        function switchChannel(channel) {
            $scope.selectedChannel.Selected = false;
            channel.Selected = true;
            $scope.selectedChannel = channel;

            $scope.pendingSave = false;
            $scope.selectedProduct.Selected = false;
            $scope.selectedProduct = {};
            $scope.selectedChannelProduct.Selected = false;
            $scope.selectedChannelProduct = {};


            var request = {
                channelId: channel.Id
            };

            Service('GetChannelWithProducts', request).then(function (data) {
                $scope.selectedChannel.Products = data.Products;

                var products = $scope.selectedChannel.Products;

                for (var i = 0; i < products.length; i++) {
                    if (products[i].ChannelHasProduct) {
                        $scope.channelProductClicked(products[i]);
                        break;
                    }
                }
            });
        }

        /*****Products******/
        function loadProducts() {
            Service('GetProducts').then(function (Products) {
                $scope.Products = Products;
            });
        }

        $scope.productClicked = function (product) {
            $scope.selectedProduct.Selected = false;
            product.Selected = true;
            $scope.selectedProduct = product;
        }

        $scope.channelProductClicked = function (channelProduct) {

            $scope.selectedChannelProduct.Selected = false;
            channelProduct.Selected = true;

            var matrix = channelProduct.PriceDefsMatrix;

            for (var i = 0; i < matrix.length; i++) {
                for (var j = 0; j < matrix[i].length; j++) {
                    matrix[i][j] = Object(matrix[i][j]);
                }
                matrix[i] = Object(matrix[i]);
            }

            $scope.selectedChannelProduct = channelProduct;
        }

        $scope.channelHasSelectedProduct = function () {
            if (_.isEmpty($scope.selectedProduct))
                return true;

            var products = $scope.selectedChannel.Products;

            for (var i = 0; i < products.length; i++)
                if (products[i].Id == $scope.selectedProduct.Id) {
                    return products[i].ChannelHasProduct;
                }

            return false;
        }


        //$scope.showNewProductInput = function () {
        //    $scope.newProductInputVisible = true;
        //}

        //$scope.hideNewProductInput = function () {
        //    $scope.newProductInputVisible = false;
        //}

        $scope.modalNewProductInput = function () {
            var modalInstance = $modal.open({
                animation: false,
                templateUrl: 'tplAddNewProduct.html',
                controller: ['$scope', '$modalInstance', 'newProductName', 'createNewProduct',
									function ($scope, $modalInstance, newProductName, createNewProduct) {

                    $scope.newProductName = newProductName;

                    $scope.cancel = function () {
                        $modalInstance.close();
                    }

                    $scope.save = function () {
                        createNewProduct($scope.newProductName);

                        $modalInstance.close();
                    }
                }],
                resolve: {
                    newProductName: function () {
                        return $scope.newProductName;
                    },
                    createNewProduct: function () {
                        return createNewProduct;
                    }
                }
            });
        }

        function createNewProduct(newProductName) {

            Service('AddProduct', { productName: newProductName }).then(function () {
                init();
            });
        }

        $scope.addSelectedProductToChannel = function () {

            $scope.pendingSave = true;

            var products = $scope.selectedChannel.Products;

            for (var i = 0; i < products.length; i++) {
                if (products[i].Id == $scope.selectedProduct.Id) {

                    products[i].ChannelHasProduct = true;
                    $scope.channelProductClicked(products[i]);

                    break;

                }
            }
        }

        /*Price definitions*/
        $scope.priceCellClicked = function (dowPrice, x, y, $event) {

            deselectAllPriceCells();

            if ($event.shiftKey) {//If shift+click then select rectangle

                var yCounter = y;

                while (true) {

                    var xCounter = x;

                    while (true) {

                        $scope.selectedChannelProduct.PriceDefsMatrix[yCounter][xCounter].Selected = true;

                        if (xCounter == $scope.firstCell.x)
                            break;

                        if (xCounter > $scope.firstCell.x) {
                            xCounter--;
                        }
                        else if (xCounter < $scope.firstCell.x) {
                            xCounter++;
                        }

                    }

                    if (yCounter == $scope.firstCell.y)
                        break;

                    if (yCounter > $scope.firstCell.y) {
                        yCounter--;
                    }
                    else if (yCounter < $scope.firstCell.y) {
                        yCounter++;
                    }

                }

            }
            else {

                $scope.firstCell = { x: x, y: y };

            }

            dowPrice.Selected = true;

        }

        function deselectAllPriceCells() {

            var matrix = $scope.selectedChannelProduct.PriceDefsMatrix;

            for (var i = 0; i < matrix.length; i++) {
                for (var j = 0; j < matrix[i].length; j++) {
                    matrix[i][j].Selected = false;
                }
            }
        }

        $scope.fillPerSecond = function () {

            var value = parseFloat($scope.spotPrice) / parseFloat($scope.spotDuration);

            fillSelectedCells(value);
        }

        $scope.fillExact = function () {

            fillSelectedCells($scope.spotPrice);
        }

        $scope.clear = function () {

            fillSelectedCells(0);
        }

        function fillSelectedCells(value) {
            $scope.pendingSave = true;

            var matrix = $scope.selectedChannelProduct.PriceDefsMatrix;

            for (var i = 0; i < matrix.length; i++) {
                for (var j = 0; j < matrix[i].length; j++) {
                    if (matrix[i][j].Selected) {
                        matrix[i][j] = Object(value);
                        matrix[i][j].Selected = true;
                        matrix[i][j].Touched = true;
                    }
                }
            }
        }

        $scope.cancel = function () {
            confirmPopup.open('Discard changes?', null, 'Do you really want to discard all changes?').then(function () {
                switchChannel($scope.selectedChannel);
            });
        }

        $scope.saveToDatabase = function () {
            confirmPopup.open('Save changes?', null, 'Do you really want to save all changes to the database?').then(function () {

                var listPriceDefs = [];

                var products = $scope.selectedChannel.Products;

                for (var i = 0; i < products.length; i++) {

                    var product = products[i];

                    if (product.ChannelHasProduct) {

                        var matrix = product.PriceDefsMatrix;

                        for (var j = 0; j < matrix.length; j++) {
                            for (var k = 0; k < matrix[j].length; k++) {
                                if (matrix[j][k].Touched) {

                                    var priceDef = {
                                        ChannelId: $scope.selectedChannel.Id,
                                        ProductId: product.Id,
                                        Hour: j,
                                        Dow: k,
                                        Pps: matrix[j][k].valueOf()
                                    };

                                    listPriceDefs.push(priceDef);
                                }
                            }
                        }
                    }
                }

                Service('AddChannelWithProducts', { priceDefs: listPriceDefs }).then(function () {
                    switchChannel($scope.selectedChannel);
                });


            });
        }

        /*************INIT*************/
        initPageLoad();

    }]);
