# Main entry point
SOURCE = app/app.js

# The target
TARGET = bridge.js

# Compilation flags
FLAGS =

# Enable sourcemap with "make sourcemap=1"
ifdef sourcemap
	FLAGS += --debug
endif

# Binaries
WATCHIFY = ./node_modules/.bin/watchify
BROWSERIFY = ./node_modules/.bin/browserify
NPM = npm

.PHONY: build clean watch

build: $(TARGET)

clean:
	rm -f $(TARGET)

watch:
	$(WATCHIFY) --verbose $(FLAGS) -o $(TARGET) -- $(SOURCE)

# Note: browserify --list is so slow, just rely on node_modules
$(TARGET): $(SOURCE) $(LIBS) node_modules
	$(BROWSERIFY) $(FLAGS) -o $@ -- $<

node_modules:
	$(NPM) install
