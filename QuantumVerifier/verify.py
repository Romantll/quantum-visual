"""
Qiskit verification harness.
Runs gate sequences through Qiskit's statevector simulator and compares
amplitudes to the C# QuantumMath library output (passed via stdin or a JSON file).

Usage:
    python verify.py             -- runs built-in test sequences and prints results
    python verify.py <file.json> -- reads gate sequences from JSON file

JSON format:
    [
      { "gates": ["H", "X", "H"], "init": "0" },
      ...
    ]
"""

import sys, json, math
from qiskit import QuantumCircuit
from qiskit_aer import AerSimulator

TOLERANCE = 1e-10

# Maps gate name -> Qiskit QuantumCircuit method and args
GATE_MAP = {
    "X": lambda qc: qc.x(0),
    "Y": lambda qc: qc.y(0),
    "Z": lambda qc: qc.z(0),
    "H": lambda qc: qc.h(0),
    "S": lambda qc: qc.s(0),
    "T": lambda qc: qc.t(0),
}

def run_sequence(gates: list[str], init: str = "0") -> tuple[complex, complex]:
    qc = QuantumCircuit(1)
    if init == "1":
        qc.x(0)
    for g in gates:
        GATE_MAP[g](qc)
    qc.save_statevector()

    sim = AerSimulator(method="statevector")
    result = sim.run(qc).result()
    sv = result.get_statevector()
    return complex(sv[0]), complex(sv[1])


def verify(sequence: dict, csharp_alpha: complex | None = None, csharp_beta: complex | None = None):
    gates = sequence["gates"]
    init  = sequence.get("init", "0")
    alpha, beta = run_sequence(gates, init)

    label = " -> ".join(gates) if gates else "I"
    init_ket = "|0>" if init == "0" else "|1>"
    print(f"[{init_ket} -> {label}]")
    print(f"  alpha = {alpha.real:+.10f} {alpha.imag:+.10f}i")
    print(f"  beta  = {beta.real:+.10f}  {beta.imag:+.10f}i")
    print(f"  |a|^2+|b|^2 = {abs(alpha)**2 + abs(beta)**2:.15f}")

    if csharp_alpha is not None and csharp_beta is not None:
        da = abs(alpha - csharp_alpha)
        db = abs(beta  - csharp_beta)
        ok = da < TOLERANCE and db < TOLERANCE
        print(f"  C# match: {'PASS' if ok else f'FAIL  da={da:.2e}  db={db:.2e}'}")
    print()
    return alpha, beta


# Built-in test sequences — the same ones the C# unit tests cover
BUILTIN_SEQUENCES = [
    {"gates": ["H"],       "init": "0"},   # |0⟩ → |+⟩
    {"gates": ["H"],       "init": "1"},   # |1⟩ → |−⟩
    {"gates": ["H","H"],   "init": "0"},   # H² = I
    {"gates": ["X"],       "init": "0"},   # X|0⟩ = |1⟩
    {"gates": ["X"],       "init": "1"},   # X|1⟩ = |0⟩
    {"gates": ["Z"],       "init": "1"},   # Z|1⟩ = -|1⟩
    {"gates": ["S"],       "init": "1"},   # S|1⟩ = i|1⟩
    {"gates": ["H","Z","H"], "init": "0"}, # HZH = X
    {"gates": ["H","X","H"], "init": "0"}, # HXH = Z
    {"gates": ["H","S","H"], "init": "0"}, # HZH chain
]


if __name__ == "__main__":
    if len(sys.argv) > 1:
        with open(sys.argv[1]) as f:
            sequences = json.load(f)
    else:
        sequences = BUILTIN_SEQUENCES

    print(f"Running {len(sequences)} sequences through Qiskit statevector simulator\n")
    for seq in sequences:
        verify(seq)

    print("Done. Paste the alpha/beta values above into your C# comparison to confirm 1e-10 agreement.")
