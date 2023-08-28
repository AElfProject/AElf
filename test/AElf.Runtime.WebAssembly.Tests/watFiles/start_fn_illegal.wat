(module
	(import "seal0" "seal_return" (func $seal_return (param i32 i32 i32)))
	(import "env" "memory" (memory 1 1))

	(start $start)
	(func $start
		(unreachable)
	)

	(func (export "call")
		(unreachable)
	)

	(func (export "deploy")
		(unreachable)
	)

	(data (i32.const 8) "\01\02\03\04")
)