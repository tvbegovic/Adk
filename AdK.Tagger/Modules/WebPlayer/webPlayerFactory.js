angular.module('app')
	.factory('webPlayerFactory', [function () {
		var factory = {};

		factory.selectedChannel = null;
		factory.channels = null;		
		factory.currentHour = null;
		factory.tags = null;
		factory.resizingEdgeRight = 1;
		factory.resizingEdgeLeft = 2;

		factory.getElementAttribute = function(elem, key) {
			var attr = elem.attributes.getNamedItem(key);
			if (attr != null)
				return attr.value;
			return null;
		}

		factory.addOrUpdateTextNode = function (parent, title, layer, onClick, onDblclick) {
			var textNode = null;
			for (var i = 0; i < parent.childNodes.length; i++) {
				var child = parent.childNodes[i];
				if (factory.hasClass(null, 'wp_waveform_segment_title', child)) {
					textNode = child;
					child.innerText = title;
					break;
				}
			}
			if (textNode == null) {
				textNode = factory.createSegmentTextNode(title, layer, onClick, onDblclick);
				parent.appendChild(textNode);
			}
		}

		factory.createSegmentTextNode = function (title, layer, onClick, onDblclick) {
			var textNode = document.createElement('div');
			textNode.innerText = title;
			textNode.className = 'wp_waveform_segment_title noselect layer' + layer;
			textNode.onclick = onClick;
			textNode.ondblclick = onDblclick;
			return textNode;
		};

		factory.addElementAttribute = function (element, key, value) {
			var attr = document.createAttribute(key);
			attr.value = value;
			element.attributes.setNamedItem(attr);
		};

		factory.removeElementAttribute = function (elem, key) {
			var attr = elem.attributes.getNamedItem(key);
			if (attr != null)
				elem.attributes.removeNamedItem(key);
		};

		factory.getElementAttribute = function (elem, key) {
			var attr = elem.attributes.getNamedItem(key);
			if (attr != null)
				return attr.value;
			return null;
		};

		factory.getSegments = function (data, layer) {
			if (layer in data.segments)
				return data.segments[layer];
			return null;
		};

		factory.getSegment = function(element, data, layer) {
			var segments = factory.getSegments(data, layer);
			var sequence = factory.getElementAttribute(element, 'segmentId');
			if (segments != null) {
				return segments.find(s => s.sequence == sequence);
			}
			return null;
		};

		factory.removeClass = function (classString, className, element) {
			if (element)
				classString = element.className;
			var arr = classString.split(' ');
			var index = arr.findIndex(s => s == className);
			var newValue = classString;
			if (index >= 0) {
				arr.splice(index, 1);
				newValue = arr.join(' ');
			}
			if (element)
				element.className = newValue;
			return newValue;
		}

		factory.addClass = function (classString, className, element) {
			if (element)
				classString = element.className;
			var arr = classString.split(' ');
			var newValue = classString;
			if (arr.indexOf(className) < 0) {
				newValue += ' ' + className;
			}
			if (element)
				element.className = newValue;
			return newValue;
		}

		factory.hasClass = function (classString, className, element) {
			if (!element.className)
				return false;
			return element.className.indexOf(className) >= 0;
		}


		return factory;
	}]);
