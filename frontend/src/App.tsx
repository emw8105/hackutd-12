import {
	type ChangeEvent,
	type FormEvent,
	useEffect,
	useRef,
	useState,
} from "react";
import "./App.css";

type BuildingConfig = {
	width: number;
	depth: number;
	floors: number;
};

type Rack = {
	id: string;
	row: number;
	column: number;
	label?: string;
};

type Technician = {
	id: number;
	row: number;
	column: number;
};

type Floor = {
	level: number;
	racks: Rack[];
	technicians: Technician[];
};

type PlacementMode = "rack" | "technician";

type SelectedCell = {
	floor: number;
	row: number;
	column: number;
} | null;

const createFloor = (level: number): Floor => ({
	level,
	racks: [],
	technicians: [],
});

const formatRackId = (
	floor: number,
	row: number,
	column: number,
	rackSequence: number,
	uSequence: number
) => {
	const hall = `${floor.toString().padStart(2, "0")}`;
	const pod = `${row.toString().padStart(2, "0")}`;
	const aisle = `${column.toString().padStart(2, "0")}`;
	const rack = `${rackSequence.toString().padStart(2, "0")}`;
	const unit = `${uSequence.toString().padStart(2, "0")}`;

	return `${hall}-${pod}-${aisle}-${rack}-${unit}`;
};

const INITIAL_CONFIG: BuildingConfig = {
	width: 6,
	depth: 4,
	floors: 3,
};

function App() {
	const [config, setConfig] = useState<BuildingConfig>(INITIAL_CONFIG);
	const [floors, setFloors] = useState<Floor[]>(() =>
		Array.from({ length: INITIAL_CONFIG.floors }, (_, index) =>
			createFloor(index + 1)
		)
	);
	const floorsRef = useRef<Floor[]>(floors);
	const rackCounterRef = useRef(1);
	const technicianCounterRef = useRef(1);
	const [placementMode, setPlacementMode] = useState<PlacementMode>("rack");
	const [selectedCell, setSelectedCell] = useState<SelectedCell>(null);

	const [rackForm, setRackForm] = useState({
		floor: 1,
		row: 1,
		column: 1,
		label: "",
	});
	const [technicianForm, setTechnicianForm] = useState({
		floor: 1,
		row: 1,
		column: 1,
	});
	const [technicianToMove, setTechnicianToMove] = useState<{
		floor: number;
		technicianId: number;
	} | null>(null);

	const [rackFeedback, setRackFeedback] = useState<{
		status: "success" | "error";
		message: string;
	} | null>(null);

	const [technicianFeedback, setTechnicianFeedback] = useState<{
		status: "success" | "error";
		message: string;
	} | null>(null);

	useEffect(() => {
		setFloors((current) => {
			if (config.floors === current.length) {
				return current;
			}

			if (config.floors > current.length) {
				const additions = Array.from(
					{ length: config.floors - current.length },
					(_, index) => createFloor(current.length + index + 1)
				);
				return [...current, ...additions];
			}

			return current.slice(0, config.floors);
		});
	}, [config.floors]);

	useEffect(() => {
		floorsRef.current = floors;
	}, [floors]);

	useEffect(() => {
		const sendDistances = async () => {
			const snapshot = floorsRef.current;
			const technicians = snapshot.flatMap((floor) =>
				floor.technicians.map((technician) => ({
					...technician,
					floor: floor.level,
				}))
			);
			const racks = snapshot.flatMap((floor) =>
				floor.racks.map((rack) => ({
					...rack,
					floor: floor.level,
				}))
			);
			const distanceMatrix = technicians.map((technician) =>
				racks.map((rack) => {
					const floorDiff = technician.floor - rack.floor;
					const rowDiff = technician.row - rack.row;
					const columnDiff = technician.column - rack.column;

					return Math.sqrt(
						floorDiff * floorDiff + rowDiff * rowDiff + columnDiff * columnDiff
					);
				})
			);

			try {
				await fetch("https://hackutd-12-production.up.railway.app/distance", {
					method: "POST",
					headers: {
						"Content-Type": "application/json",
					},
					body: JSON.stringify(distanceMatrix),
				});
			} catch (error) {
				console.error("Failed to send distance matrix", error);
			}
		};

		const intervalId = window.setInterval(() => {
			void sendDistances();
		}, 1000);

		void sendDistances();

		return () => {
			window.clearInterval(intervalId);
		};
	}, []);

	useEffect(() => {
		setRackForm((previous) => ({
			...previous,
			floor: Math.min(Math.max(previous.floor, 1), config.floors),
			row: Math.min(Math.max(previous.row, 1), config.depth),
			column: Math.min(Math.max(previous.column, 1), config.width),
		}));

		setTechnicianForm((previous) => ({
			...previous,
			floor: Math.min(Math.max(previous.floor, 1), config.floors),
			row: Math.min(Math.max(previous.row, 1), config.depth),
			column: Math.min(Math.max(previous.column, 1), config.width),
		}));

		setSelectedCell((previous) => {
			if (!previous) {
				return previous;
			}

			const nextFloor = Math.min(Math.max(previous.floor, 1), config.floors);
			const nextRow = Math.min(Math.max(previous.row, 1), config.depth);
			const nextColumn = Math.min(Math.max(previous.column, 1), config.width);

			if (
				nextFloor !== previous.floor ||
				nextRow !== previous.row ||
				nextColumn !== previous.column
			) {
				return {
					floor: nextFloor,
					row: nextRow,
					column: nextColumn,
				};
			}

			return previous;
		});
	}, [config.floors, config.depth, config.width]);

	const handleConfigChange =
		(key: keyof BuildingConfig) => (event: ChangeEvent<HTMLInputElement>) => {
			const value = Number.parseInt(event.target.value, 10);
			const sanitized = Number.isNaN(value) ? 1 : Math.max(1, value);
			setConfig((previous) => ({
				...previous,
				[key]: sanitized,
			}));
		};

	const handleRackFieldChange = (
		event: ChangeEvent<HTMLInputElement | HTMLSelectElement>
	) => {
		const { name, value } = event.target;
		setRackForm((previous) => ({
			...previous,
			[name]:
				name === "label"
					? value
					: Math.min(
							Math.max(Number.parseInt(value, 10) || 1, 1),
							name === "row"
								? config.depth
								: name === "column"
								? config.width
								: config.floors
					  ),
		}));
	};

	const handleTechnicianFieldChange = (
		event: ChangeEvent<HTMLInputElement | HTMLSelectElement>
	) => {
		const { name, value } = event.target;
		const numericValue = Number.parseInt(value, 10);
		const sanitizedNumber = Math.min(
			Math.max(Number.isNaN(numericValue) ? 1 : numericValue, 1),
			name === "row"
				? config.depth
				: name === "column"
				? config.width
				: config.floors
		);

		setTechnicianForm((previous) => ({
			...previous,
			[name]: sanitizedNumber,
		}));
	};

	const handlePlacementModeChange = (mode: PlacementMode) => {
		setPlacementMode(mode);
		setRackFeedback(null);
		setTechnicianFeedback(null);

		if (mode !== "technician") {
			setTechnicianToMove(null);
		}

		if (mode === "rack" && selectedCell) {
			const targetFloor = floors.find(
				(item) => item.level === selectedCell.floor
			);
			const occupied = targetFloor?.racks.some(
				(rack) =>
					rack.row === selectedCell.row && rack.column === selectedCell.column
			);

			if (occupied) {
				setSelectedCell(null);
			}
		}
	};

	const handleStartTechnicianMove = (
		floorLevel: number,
		technicianId: number
	) => {
		setPlacementMode("technician");
		setTechnicianToMove({ floor: floorLevel, technicianId });
		setRackFeedback(null);
		setTechnicianFeedback(null);
		setSelectedCell(null);
	};

	const cancelTechnicianMove = () => {
		setTechnicianToMove(null);
		setTechnicianFeedback(null);
	};

	const completeTechnicianMove = (
		targetFloor: number,
		row: number,
		column: number
	) => {
		if (!technicianToMove) {
			return;
		}

		if (row < 1 || row > config.depth || column < 1 || column > config.width) {
			setTechnicianFeedback({
				status: "error",
				message: "Technician coordinates are outside the building footprint.",
			});
			return;
		}

		let movedTechnicianId: number | null = null;
		let success = false;
		let errorMessage: string | null = null;

		setFloors((previous) => {
			const originFloor = previous.find(
				(floor) => floor.level === technicianToMove.floor
			);

			if (!originFloor) {
				errorMessage = "Original technician location could not be found.";
				return previous;
			}

			const technician = originFloor.technicians.find(
				(item) => item.id === technicianToMove.technicianId
			);

			if (!technician) {
				errorMessage = "Technician record no longer exists.";
				return previous;
			}

			const destinationExists = previous.some(
				(floor) => floor.level === targetFloor
			);

			if (!destinationExists) {
				errorMessage = "Target floor does not exist.";
				return previous;
			}

			movedTechnicianId = technician.id;
			const updatedTechnician: Technician = {
				...technician,
				row,
				column,
			};

			success = true;

			if (technicianToMove.floor === targetFloor) {
				return previous.map((floor) =>
					floor.level === targetFloor
						? {
								...floor,
								technicians: floor.technicians.map((item) =>
									item.id === technician.id ? updatedTechnician : item
								),
						  }
						: floor
				);
			}

			return previous.map((floor) => {
				if (floor.level === technicianToMove.floor) {
					return {
						...floor,
						technicians: floor.technicians.filter(
							(item) => item.id !== technician.id
						),
					};
				}

				if (floor.level === targetFloor) {
					return {
						...floor,
						technicians: [...floor.technicians, updatedTechnician],
					};
				}

				return floor;
			});
		});

		if (success && movedTechnicianId !== null) {
			setTechnicianFeedback({
				status: "success",
				message: `Technician #${movedTechnicianId} moved to floor ${targetFloor} at (${row}, ${column}).`,
			});
			setTechnicianToMove(null);
			setSelectedCell(null);
		} else if (errorMessage) {
			setTechnicianFeedback({
				status: "error",
				message: errorMessage,
			});
			setTechnicianToMove(null);
		}
	};

	const handleCellSelection = (
		floorLevel: number,
		row: number,
		column: number,
		hasRack: boolean
	) => {
		setRackFeedback(null);
		setTechnicianFeedback(null);

		if (placementMode === "rack") {
			if (hasRack) {
				setRackFeedback({
					status: "error",
					message: `Position (${row}, ${column}) on floor ${floorLevel} is already occupied.`,
				});
				return;
			}

			setRackForm((previous) => ({
				...previous,
				floor: floorLevel,
				row,
				column,
			}));
		} else {
			if (technicianToMove) {
				completeTechnicianMove(floorLevel, row, column);
				return;
			}

			setTechnicianForm((previous) => ({
				...previous,
				floor: floorLevel,
				row,
				column,
			}));
		}

		setSelectedCell({
			floor: floorLevel,
			row,
			column,
		});
	};

	const attemptPlaceRack = (
		floor: number,
		row: number,
		column: number,
		labelInput: string
	) => {
		setRackFeedback(null);

		const targetFloorIndex = floors.findIndex((item) => item.level === floor);

		if (targetFloorIndex === -1) {
			setRackFeedback({
				status: "error",
				message: "Selected floor does not exist.",
			});
			return false;
		}

		const targetFloor = floors[targetFloorIndex];
		const rackOccupied = targetFloor.racks.some(
			(rack) => rack.row === row && rack.column === column
		);

		if (rackOccupied) {
			setRackFeedback({
				status: "error",
				message: `Position (${row}, ${column}) on floor ${floor} is already occupied.`,
			});
			return false;
		}

		const trimmedLabel = labelInput.trim();
		const rackSequence = targetFloor.racks.length + 1;
		const uSequence = rackCounterRef.current;
		rackCounterRef.current += 1;
		const rackId = formatRackId(floor, row, column, rackSequence, uSequence);

		const newRack: Rack = {
			id: rackId,
			row,
			column,
			label: trimmedLabel || undefined,
		};

		setFloors((previous) =>
			previous.map((item) =>
				item.level === floor
					? {
							...item,
							racks: [...item.racks, newRack],
					  }
					: item
			)
		);

		setRackFeedback({
			status: "success",
			message: `Rack ${newRack.id} placed on floor ${floor} at (${row}, ${column}).`,
		});

		setRackForm((form) => ({
			...form,
			label: "",
		}));
		setSelectedCell(null);

		return true;
	};

	const handleRackSubmit = (event: FormEvent<HTMLFormElement>) => {
		event.preventDefault();

		const { floor, row, column, label } = rackForm;
		attemptPlaceRack(floor, row, column, label);
	};

	const attemptAssignTechnician = (
		floor: number,
		row: number,
		column: number
	) => {
		setTechnicianFeedback(null);

		if (technicianToMove) {
			setTechnicianFeedback({
				status: "error",
				message:
					"Finish moving the current technician before placing a new one.",
			});
			return false;
		}

		if (row < 1 || row > config.depth || column < 1 || column > config.width) {
			setTechnicianFeedback({
				status: "error",
				message: "Technician coordinates are outside the building footprint.",
			});
			return false;
		}

		const targetFloorIndex = floors.findIndex((item) => item.level === floor);

		if (targetFloorIndex === -1) {
			setTechnicianFeedback({
				status: "error",
				message: "Selected floor does not exist.",
			});
			return false;
		}

		const technicianId = technicianCounterRef.current;
		technicianCounterRef.current += 1;

		const newTechnician: Technician = {
			id: technicianId,
			row,
			column,
		};

		setFloors((previous) =>
			previous.map((item) =>
				item.level === floor
					? {
							...item,
							technicians: [...item.technicians, newTechnician],
					  }
					: item
			)
		);

		setTechnicianFeedback({
			status: "success",
			message: `Technician #${technicianId} assigned to floor ${floor} at (${row}, ${column}).`,
		});

		setSelectedCell(null);

		return true;
	};

	const handleTechnicianSubmit = (event: FormEvent<HTMLFormElement>) => {
		event.preventDefault();

		const { floor, row, column } = technicianForm;
		attemptAssignTechnician(floor, row, column);
	};

	const floorsToRender = floors.slice(0, config.floors);
	const floorLevels = floorsToRender.map((floor) => floor.level);
	const isTechnicianMoving = Boolean(technicianToMove);
	const nextTechnicianId = technicianCounterRef.current;

	return (
		<div className="app-shell">
			<header className="app-header">
				<div>
					<h1>Data Center Constructor</h1>
					<p>
						Configure the building shell, place server racks with unique IDs,
						and assign technicians to the right floors.
					</p>
				</div>
				<div className="building-meta">
					<span>
						Footprint:{" "}
						<strong>
							{config.width} × {config.depth}
						</strong>{" "}
						racks per floor
					</span>
					<span>
						Floors: <strong>{config.floors}</strong>
					</span>
				</div>
			</header>

			<main className="app-layout">
				<section className="controls-panel">
					<div className="form-section">
						<h2>Placement Mode</h2>
						<div
							className="placement-toggle"
							role="group"
							aria-label="Placement mode"
						>
							<button
								type="button"
								className={`toggle-button ${
									placementMode === "rack" ? "active" : ""
								}`}
								onClick={() => handlePlacementModeChange("rack")}
								aria-pressed={placementMode === "rack"}
							>
								Place Rack
							</button>
							<button
								type="button"
								className={`toggle-button ${
									placementMode === "technician" ? "active" : ""
								}`}
								onClick={() => handlePlacementModeChange("technician")}
								aria-pressed={placementMode === "technician"}
							>
								Assign Technician
							</button>
						</div>
						{selectedCell ? (
							<div
								className={`selection-banner ${
									placementMode === "technician" ? "technician" : "rack"
								}`}
							>
								<span>
									Floor {selectedCell.floor} • Row {selectedCell.row} • Column{" "}
									{selectedCell.column}
								</span>
								{placementMode === "rack" ? (
									<>
										<span>
											Add an optional label and place a rack instantly.
										</span>
										<div className="selection-actions">
											<button
												type="button"
												className="secondary-button"
												onClick={() =>
													attemptPlaceRack(
														selectedCell.floor,
														selectedCell.row,
														selectedCell.column,
														rackForm.label
													)
												}
											>
												Place Rack Here
											</button>
										</div>
									</>
								) : isTechnicianMoving ? (
									<>
										<span>
											Select a new location for technician #
											{technicianToMove?.technicianId}.
										</span>
										<div className="selection-actions">
											<button
												type="button"
												className="secondary-button"
												onClick={cancelTechnicianMove}
											>
												Cancel Move
											</button>
										</div>
									</>
								) : (
									<>
										<span>
											Place technician #{nextTechnicianId} at this location.
										</span>
										<div className="selection-actions">
											<button
												type="button"
												className="secondary-button"
												onClick={() =>
													attemptAssignTechnician(
														selectedCell.floor,
														selectedCell.row,
														selectedCell.column
													)
												}
											>
												Place Technician Here
											</button>
										</div>
									</>
								)}
							</div>
						) : (
							<p className="selection-hint">
								Click a cell in the floor grid to prefill the forms below.
							</p>
						)}
					</div>

					<div className="form-section">
						<h2>Building Configuration</h2>
						<form
							className="form-grid"
							onSubmit={(event) => event.preventDefault()}
						>
							<label className="field">
								<span>Width (columns)</span>
								<input
									type="number"
									value={config.width}
									onChange={handleConfigChange("width")}
								/>
							</label>
							<label className="field">
								<span>Depth (rows)</span>
								<input
									type="number"
									value={config.depth}
									onChange={handleConfigChange("depth")}
								/>
							</label>
							<label className="field">
								<span>Floors</span>
								<input
									type="number"
									value={config.floors}
									onChange={handleConfigChange("floors")}
								/>
							</label>
						</form>
					</div>

					<div className="form-section">
						<h2>Place Server Rack</h2>
						<form className="form-grid" onSubmit={handleRackSubmit}>
							<label className="field">
								<span>Floor</span>
								<select
									name="floor"
									value={rackForm.floor}
									onChange={handleRackFieldChange}
								>
									{floorLevels.map((level) => (
										<option key={level} value={level}>
											Floor {level}
										</option>
									))}
								</select>
							</label>
							<label className="field">
								<span>Row</span>
								<input
									name="row"
									type="number"
									max={config.depth}
									value={rackForm.row}
									onChange={handleRackFieldChange}
								/>
							</label>
							<label className="field">
								<span>Column</span>
								<input
									name="column"
									type="number"
									max={config.width}
									value={rackForm.column}
									onChange={handleRackFieldChange}
								/>
							</label>
							<label className="field field-span">
								<span>Label (optional)</span>
								<input
									name="label"
									type="text"
									placeholder="Rack cluster or purpose"
									value={rackForm.label}
									onChange={handleRackFieldChange}
								/>
							</label>
							<button type="submit" className="primary-button">
								Place Rack
							</button>
						</form>
						{rackFeedback && (
							<p className={`feedback ${rackFeedback.status}`}>
								{rackFeedback.message}
							</p>
						)}
					</div>

					<div className="form-section">
						<h2>Assign Technician</h2>
						<form className="form-grid" onSubmit={handleTechnicianSubmit}>
							<p className="form-hint">
								Technicians are auto-numbered. Pick a floor and coordinates to
								place them.
							</p>
							<label className="field">
								<span>Floor</span>
								<select
									name="floor"
									value={technicianForm.floor}
									onChange={handleTechnicianFieldChange}
								>
									{floorLevels.map((level) => (
										<option key={level} value={level}>
											Floor {level}
										</option>
									))}
								</select>
							</label>
							<label className="field">
								<span>Row</span>
								<input
									name="row"
									type="number"
									min={1}
									max={config.depth}
									value={technicianForm.row}
									onChange={handleTechnicianFieldChange}
								/>
							</label>
							<label className="field">
								<span>Column</span>
								<input
									name="column"
									type="number"
									min={1}
									max={config.width}
									value={technicianForm.column}
									onChange={handleTechnicianFieldChange}
								/>
							</label>
							<button
								type="submit"
								className="primary-button"
								disabled={isTechnicianMoving}
								title={
									isTechnicianMoving
										? "Finish moving the current technician first."
										: undefined
								}
							>
								Assign Technician
							</button>
						</form>
						{technicianFeedback && (
							<p className={`feedback ${technicianFeedback.status}`}>
								{technicianFeedback.message}
							</p>
						)}
					</div>
				</section>

				<section className="floors-overview">
					{floorsToRender.map((floor) => (
						<article key={floor.level} className="floor-card">
							<header className="floor-header">
								<h3>Floor {floor.level}</h3>
								<div className="floor-stats">
									<span>
										Racks: <strong>{floor.racks.length}</strong>
									</span>
									<span>
										Technicians: <strong>{floor.technicians.length}</strong>
									</span>
								</div>
							</header>
							<div
								className="floor-grid"
								style={{
									gridTemplateColumns: `repeat(${config.width}, minmax(0, 1fr))`,
								}}
							>
								{Array.from(
									{ length: config.depth * config.width },
									(_, index) => {
										const row = Math.floor(index / config.width) + 1;
										const column = (index % config.width) + 1;
										const rack = floor.racks.find(
											(item) => item.row === row && item.column === column
										);
										const hasRack = Boolean(rack);
										const techniciansInCell = floor.technicians.filter(
											(technician) =>
												technician.row === row && technician.column === column
										);
										const hasTechnicians = techniciansInCell.length > 0;
										const isSelected =
											selectedCell?.floor === floor.level &&
											selectedCell?.row === row &&
											selectedCell?.column === column;
										const cellClasses = [
											"floor-cell",
											hasRack ? "occupied" : "",
											hasTechnicians ? "tech-present" : "",
											"interactive",
											isSelected ? "selected" : "",
											isSelected && placementMode === "technician"
												? "selected-technician"
												: "",
										]
											.filter(Boolean)
											.join(" ");

										return (
											<div
												key={`${row}-${column}`}
												className={cellClasses}
												role="button"
												tabIndex={0}
												onClick={() =>
													handleCellSelection(floor.level, row, column, hasRack)
												}
												onKeyDown={(event) => {
													if (event.key === "Enter" || event.key === " ") {
														event.preventDefault();
														handleCellSelection(
															floor.level,
															row,
															column,
															hasRack
														);
													}
												}}
												aria-label={`Floor ${
													floor.level
												}, row ${row}, column ${column}${
													rack ? `, rack ${rack.id}` : ""
												}${
													hasTechnicians
														? `, ${techniciansInCell.length} technician${
																techniciansInCell.length > 1 ? "s" : ""
														  }`
														: ""
												}`}
												aria-pressed={isSelected}
											>
												{rack ? (
													<>
														<span className="rack-id">{rack.id}</span>
														{rack.label && (
															<span className="rack-label">{rack.label}</span>
														)}
														<span className="rack-coordinates">
															({row}, {column})
														</span>
														{hasTechnicians && (
															<div className="technician-tags">
																{techniciansInCell.slice(0, 3).map((tech) => (
																	<span
																		key={tech.id}
																		className="technician-tag"
																	>
																		T{tech.id}
																	</span>
																))}
																{techniciansInCell.length > 3 && (
																	<span className="technician-tag more">
																		+{techniciansInCell.length - 3}
																	</span>
																)}
															</div>
														)}
													</>
												) : (
													<span className="cell-coordinates">
														({row}, {column})
													</span>
												)}
												{!rack && hasTechnicians && (
													<div className="technician-tags">
														{techniciansInCell.slice(0, 3).map((tech) => (
															<span key={tech.id} className="technician-tag">
																T{tech.id}
															</span>
														))}
														{techniciansInCell.length > 3 && (
															<span className="technician-tag more">
																+{techniciansInCell.length - 3}
															</span>
														)}
													</div>
												)}
											</div>
										);
									}
								)}
							</div>
							<div className="technicians-wrapper">
								<h4>Technicians</h4>
								{floor.technicians.length === 0 ? (
									<p className="empty-state">No technicians assigned yet.</p>
								) : (
									<ul>
										{floor.technicians.map((technician) => {
											const isBeingMoved =
												technicianToMove?.technicianId === technician.id &&
												technicianToMove?.floor === floor.level;

											return (
												<li
													key={technician.id}
													className={isBeingMoved ? "technician-moving" : ""}
												>
													<div className="technician-line">
														<span className="technician-name">
															Technician #{technician.id}
														</span>
														<div className="technician-actions">
															<button
																type="button"
																className="link-button"
																onClick={() =>
																	handleStartTechnicianMove(
																		floor.level,
																		technician.id
																	)
																}
																disabled={isBeingMoved}
															>
																{isBeingMoved ? "Select Location" : "Move"}
															</button>
														</div>
													</div>
													<span className="technician-location">
														Row {technician.row} • Column {technician.column}
													</span>
												</li>
											);
										})}
									</ul>
								)}
							</div>
						</article>
					))}
				</section>
			</main>
		</div>
	);
}

export default App;
