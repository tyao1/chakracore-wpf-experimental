(() => {
   function _callNative(name, callback, args, ) {
	   var moduleId = bridge.functions[name];
		if (callback) {
			callbacks.push(callback);
			bridge.callNative(moduleId, callbackCount++, args);
		} else {
			bridge.callNative(moduleId, undefined, args);
		}
	}


	function naive(string, callBack) {
		_callNative('naive', callBack, string);
	}

	function request(url, callBack) {
		_callNative('request', callBack, url);
	}

	function setTitle(title) {
		_callNative('setTitle', null, title);
	}

	naive('Too young, too simple', (result) => {
		console.log('wtf', result);
	});
	naive('Im angry', (result) => {
		console.log('wtf', result);
	});
	naive('excited!', (result) => {
		console.log('wtf', result);
	});

	let componentId = 0;
	function h(componentName, props, children) {
		var id = bridge.components[componentName];
		if (id === undefined) {
			console.warn('Component ' + componentName + ' Not Found');
		}
		return {
			// _id: componentId,
			nativeId: id,
			componentName,
			props,
			children,
		};
	}

	function unmount(node) {
		if (node.children) {
			for (var child of node.children) {
				unmount(child);
			}
		}
		bridge.unmount(node._parentId, node._id);
	}

	function render(node, parentId, parentStyle) {
		if (!parentStyle) {
			parentStyle = {
				top: 0,
				left: 0,
				width: node.props.width,
				height: node.props.height,

			}; // init currentStyle
		}

		var finalProps = Object.assign({}, node.props);
		// var props = node.props;
		
		finalProps.top = parentStyle.top + (finalProps.top || 0);
		parentStyle.top += (finalProps.height || 0);

		// console.log(parentStyle.width, finalProps.width);
		if (finalProps.alignItem) {
			if (finalProps.alignItem === 'center') {
				if (finalProps.width < parentStyle.width) {
					finalProps.left = (parentStyle.width - finalProps.width) / 2;
				}
			}
		}
		if (finalProps.marginBottom) {
			parentStyle.top += finalProps.marginBottom;
		}
		if (!finalProps.width) {
			finalProps.width = parentStyle.width - parentStyle.left;
		}

		// console.log(parentId + ":" + JSON.stringify(finalProps));

		// mount the shit
		var currentId = componentId++;
		node._id = currentId;
		node._parentId = parentId;
		bridge.mount(node.nativeId, finalProps, currentId, parentId);
		if (node.children) {
			var nextStyle = {
				top: 0,
				left: 0,
				width: node.props.width,
				height: node.props.height,
			};
			for(const child of node.children) {
				render(child, currentId, nextStyle);
			}
		}
	}

	var elemTree;
	function loadDetail(id, title) {
		setTitle(title);

		if (elemTree) {
			unmount(elemTree);
			elemTree = null;
		}
		var initScreen = h('View', {
			height: 800,
			width: 600,
			top: 12,
		}, [
			h('Text', {
				text: '载入中',
				height:30,
				width: 100,
				top: 24,
				alignItem: 'center',
			})]
		);
		render(initScreen);
		elemTree = h('ScrollView', {
			height: 500,
			width: 600,
			top: 12,
			left: 12,
		}, [
			h('Button', {
				width: 100,
				height: 50,
				alignItem: 'center',
				onClick: loadData,
				text: '闷声大发财',
			}),
			h('WebView', {
				height: 500,
				width: 600,
			    src: 'http://news.tianyu.xyz/read/' + id,

			})
		]);
		
		unmount(initScreen);

		render(elemTree);
	}
	function loadData() {
		setTitle('闷声发大财');

		if (elemTree) {
			unmount(elemTree);
			elemTree = null;
		}
		var initScreen = h('View', {
			height: 800,
			width: 600,
			top: 12,
		}, [
			h('Text', {
				text: '载入中',
				height:30,
				width: 100,
				top: 24,
				alignItem: 'center',
			})]
		);
		render(initScreen);

		request('http://news.tianyu.xyz/list/%E7%BD%91%E6%98%93%E6%96%B0%E9%97%BB%2C%E5%BE%B7%E5%9B%BD%E4%B9%8B%E5%A3%B0%2C%E7%BA%BD%E7%BA%A6%E6%97%B6%E6%8A%A5%2CBBC%E4%B8%AD%E6%96%87%E7%BD%91%2C%E7%BD%91%E6%98%93%2CSolidot%2C%E5%95%86%E4%B8%9A%E4%BB%B7%E5%80%BC%2C%E7%9F%A5%E4%B9%8E%E6%97%A5%E6%8A%A5/', result => {
			// console.log('requested', result);
			const news = JSON.parse(result);
			var children = [
			];
			

			for (const data of news) {
				/*
				children.add(h('View', {
					width: 800,
					height: 200
				}))*/
				children.push(h('Text', {
					text: data.title,
					fontSize: 16,
					height: 30,
					onClick: () => {
						console.log(data.title);
						loadDetail(data._id, data.title);
					}
				}));
				children.push(h('Text', {
					text: data.source,
					fontSize: 10,
					height: 22,
					marginBottom: 6,
				}))

				// console.log(JSON.stringify(data));
			}
			children = children.concat([
				h('Button', {
					text: 'Im angry!',
					width: 200,
					height: 50,
					alignItem: 'center',
					horizontalCenter: true,
					onClick: loadData
				}),
				h('Text', {
					text: '将来报道出偏差，你们也是要负责的，知道吗!',
					height: 44,
					fontSize: 22,
				}),
				h('Text', {
					text: '江信江疑',
					fontSize: 20,
					height: 44,
				}),
				h('Text', {
					text: '你们这样子啊是不行的!',
					fontSize: 16,
					height: 38,
				}),
				h('Image', {
					height: 200,
					width: 300,
					alignItem: 'center',
					src: 'http://static01.nyt.com/images/2014/08/14/world/cn-jiang4-copy/14sino-spider01-jumbo.jpg'
				})
			
			]);

			elemTree = h('ScrollView', {
				height: 500,
				width: 600,
				top: 12,
				left: 12,
			}, children);
			
			unmount(initScreen);

			render(elemTree);
			bridge._callNative
		});
	}
	loadData();

  	return 12345;
})()