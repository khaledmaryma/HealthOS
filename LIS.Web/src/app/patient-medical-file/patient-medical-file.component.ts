import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { ApiEndpointsService } from '../api/api-endpoints.service';

interface VitalSign {
  id: number;
  patientId: number;
  vitalSignType?: number;
  vitalSignTypeDescription?: string;
  value?: string;
  date?: string;
  comment?: string;
}

interface Medication {
  id: number;
  patientId: number;
  medicationName?: string;
  dosage?: string;
  frequency?: string;
  startDate?: string;
  endDate?: string;
  status?: string;
  comment?: string;
}

interface Order {
  id: number;
  patientId: number;
  admissionId: number;
  admissionNumber?: string;
  requestType: number;
  requestTypeDescription?: string;
  requestDate: string;
  status: number;
  comments?: string;
  requestedBy: number;
  caseNumber?: number;
}

interface ProgressNote {
  id: number;
  patientId: number;
  noteDate?: string;
  noteText?: string;
  createdBy?: number;
  createdDate?: string;
}

interface ClinicalExamination {
  id: number;
  patientId: number;
  examinationDate?: string;
  examinationText?: string;
  createdBy?: number;
  createdDate?: string;
}

interface PatientHistory {
  id: number;
  patientId: number;
  historyText?: string;
  historyDate?: string;
  createdDate?: string;
}

interface RiskFactor {
  id: number;
  patientId: number;
  riskFactorId?: number;
  riskFactorDescription?: string;
  comment?: string;
}

interface CurrentIllness {
  id: number;
  patientId: number;
  illnessDescription?: string;
  startDate?: string;
  endDate?: string;
  comment?: string;
}

interface CardiacHistory {
  id: number;
  patientId: number;
  cardiacHistoryId?: number;
  cardiacHistoryDescription?: string;
  comment?: string;
  historyDate?: string;
}

interface MedicationHistory {
  id: number;
  patientId: number;
  medicationName?: string;
  dosage?: string;
  frequency?: string;
  startDate?: string;
  endDate?: string;
  comment?: string;
}

interface MedicalFile {
  vitalSigns: VitalSign[];
  medications: Medication[];
  orders: Order[];
  progressNotes: ProgressNote[];
  clinicalExaminations: ClinicalExamination[];
  patientHistory: PatientHistory[];
  riskFactors: RiskFactor[];
  currentIllness: CurrentIllness[];
  cardiacHistory: CardiacHistory[];
  medicationHistory: MedicationHistory[];
}

@Component({
  selector: 'app-patient-medical-file',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './patient-medical-file.component.html',
  styleUrl: './patient-medical-file.component.scss'
})
export class PatientMedicalFileComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);
  private readonly apiUrl = inject(ApiEndpointsService).patientMedicalFile;

  readonly patientId = signal<number | null>(null);
  readonly admissionId = signal<number | null>(null);
  readonly patientName = signal<string>('');
  readonly admissionNumber = signal<string>('');
  readonly medicalFile = signal<MedicalFile | null>(null);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly activeTab = signal<string>('vital-signs');
  
  // Modal states
  readonly showAddVitalSignModal = signal(false);
  readonly showAddProgressNoteModal = signal(false);
  readonly showAddClinicalExamModal = signal(false);
  readonly showAddMedicationModal = signal(false);
  readonly showAddRiskFactorModal = signal(false);
  readonly showAddCurrentIllnessModal = signal(false);
  readonly showAddCardiacHistoryModal = signal(false);
  readonly showAddMedicationHistoryModal = signal(false);
  readonly showAddPatientHistoryModal = signal(false);
  readonly isSaving = signal(false);
  
  // Form data - using regular properties for two-way binding
  newVitalSign = {
    vitalSignTypeID: null as number | null,
    value: null as number | null,
    dateTaken: new Date().toISOString().slice(0, 16),
    notes: ''
  };
  
  newProgressNote = {
    comments: '',
    date: new Date().toISOString().slice(0, 16),
    physicianID: 1 // Default, should be from user context
  };
  
  newClinicalExam = {
    comments: ''
  };

  newMedication = {
    productName: '',
    quantity: null as number | null,
    direction: '',
    scheduledDate: new Date().toISOString().slice(0, 16),
    status: 1,
    comment: ''
  };

  newRiskFactor = {
    riskFactorID: null as number | null,
    notes: ''
  };

  newCurrentIllness = {
    comments: ''
  };

  newCardiacHistory = {
    cardiacHistoryID: null as number | null,
    comments: ''
  };

  newMedicationHistory = {
    strength: '',
    notes: ''
  };

  newPatientHistory = {
    illnessHistoryNotes: '',
    medicationHistoryNotes: '',
    cardiacHistoryNotes: '',
    riskFactorNotes: '',
    allergyNotes: '',
    familyHistoryNotes: ''
  };

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      const patientId = params['patientId'] ? +params['patientId'] : null;
      const admissionId = params['admissionId'] ? +params['admissionId'] : null;

      if (patientId) {
        this.patientId.set(patientId);
        this.loadMedicalFileByPatientId(patientId);
      } else if (admissionId) {
        this.admissionId.set(admissionId);
        this.loadMedicalFileByAdmissionId(admissionId);
      } else {
        this.errorMessage.set('Patient ID or Admission ID is required');
      }
    });

    // Get patient info from query params
    this.route.queryParams.subscribe(params => {
      if (params['patientName']) {
        this.patientName.set(params['patientName']);
      }
      if (params['admissionNumber']) {
        this.admissionNumber.set(params['admissionNumber']);
      }
    });
  }

  loadMedicalFileByPatientId(patientId: number): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.http.get<MedicalFile>(`${this.apiUrl}/patient/${patientId}`).subscribe({
      next: (data) => {
        this.medicalFile.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading medical file:', err);
        this.errorMessage.set('Failed to load medical file. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  loadMedicalFileByAdmissionId(admissionId: number): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.http.get<MedicalFile>(`${this.apiUrl}/admission/${admissionId}`).subscribe({
      next: (data) => {
        this.medicalFile.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading medical file:', err);
        this.errorMessage.set('Failed to load medical file. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  setActiveTab(tab: string): void {
    this.activeTab.set(tab);
  }

  formatDate(dateString?: string): string {
    if (!dateString) return '-';
    try {
      const date = new Date(dateString);
      return date.toLocaleString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return dateString;
    }
  }

  formatDateOnly(dateString?: string): string {
    if (!dateString) return '-';
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
      });
    } catch {
      return dateString;
    }
  }

  goBack(): void {
    this.router.navigate(['/resident-patients']);
  }

  // Modal methods
  openAddVitalSignModal(): void {
    this.newVitalSign = {
      vitalSignTypeID: null,
      value: null,
      dateTaken: new Date().toISOString().slice(0, 16),
      notes: ''
    };
    this.showAddVitalSignModal.set(true);
  }

  closeAddVitalSignModal(): void {
    this.showAddVitalSignModal.set(false);
  }

  openAddProgressNoteModal(): void {
    this.newProgressNote = {
      comments: '',
      date: new Date().toISOString().slice(0, 16),
      physicianID: 1
    };
    this.showAddProgressNoteModal.set(true);
  }

  closeAddProgressNoteModal(): void {
    this.showAddProgressNoteModal.set(false);
  }

  openAddClinicalExamModal(): void {
    this.newClinicalExam = {
      comments: ''
    };
    this.showAddClinicalExamModal.set(true);
  }

  closeAddClinicalExamModal(): void {
    this.showAddClinicalExamModal.set(false);
  }

  // Save methods
  saveVitalSign(): void {
    const patientId = this.patientId();
    if (!patientId) {
      alert('Patient ID is required');
      return;
    }

    this.isSaving.set(true);
    const data = {
      patientID: patientId,
      vitalSignTypeID: this.newVitalSign.vitalSignTypeID,
      value: this.newVitalSign.value,
      dateTaken: this.newVitalSign.dateTaken ? new Date(this.newVitalSign.dateTaken).toISOString() : new Date().toISOString(),
      notes: this.newVitalSign.notes || null,
      createdBy: 1, // Should be from user context
      medicalUnit: 1 // Should be from admission/context
    };

    this.http.post(`${this.apiUrl}/vitalsign`, data).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeAddVitalSignModal();
        // Reload medical file
        const currentPatientId = this.patientId();
        const currentAdmissionId = this.admissionId();
        if (currentPatientId) {
          this.loadMedicalFileByPatientId(currentPatientId);
        } else if (currentAdmissionId) {
          this.loadMedicalFileByAdmissionId(currentAdmissionId);
        }
      },
      error: (err) => {
        console.error('Error saving vital sign:', err);
        alert('Failed to save vital sign. Please try again.');
        this.isSaving.set(false);
      }
    });
  }

  saveProgressNote(): void {
    const patientId = this.patientId();
    const admissionId = this.admissionId();
    if (!patientId) {
      alert('Patient ID is required');
      return;
    }

    this.isSaving.set(true);
    const data = {
      patientID: patientId,
      admissionID: admissionId,
      physicianID: this.newProgressNote.physicianID,
      comments: this.newProgressNote.comments,
      commentsHTML: this.newProgressNote.comments,
      date: this.newProgressNote.date ? new Date(this.newProgressNote.date).toISOString() : new Date().toISOString(),
      createdBy: 1, // Should be from user context
      medicalUnitID: admissionId || null
    };

    this.http.post(`${this.apiUrl}/progressnote`, data).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeAddProgressNoteModal();
        // Reload medical file
        if (patientId) {
          this.loadMedicalFileByPatientId(patientId);
        }
      },
      error: (err) => {
        console.error('Error saving progress note:', err);
        alert('Failed to save progress note. Please try again.');
        this.isSaving.set(false);
      }
    });
  }

  saveClinicalExamination(): void {
    const patientId = this.patientId();
    const admissionId = this.admissionId();
    if (!patientId) {
      alert('Patient ID is required');
      return;
    }

    this.isSaving.set(true);
    const data = {
      patientID: patientId,
      admissionID: admissionId,
      comments: this.newClinicalExam.comments,
      commentsHTML: this.newClinicalExam.comments,
      createdBy: 1 // Should be from user context
    };

    this.http.post(`${this.apiUrl}/clinicalexamination`, data).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeAddClinicalExamModal();
        // Reload medical file
        if (patientId) {
          this.loadMedicalFileByPatientId(patientId);
        }
      },
      error: (err) => {
        console.error('Error saving clinical examination:', err);
        alert('Failed to save clinical examination. Please try again.');
        this.isSaving.set(false);
      }
    });
  }

  // Additional modal methods
  openAddMedicationModal(): void {
    this.newMedication = {
      productName: '',
      quantity: null,
      direction: '',
      scheduledDate: new Date().toISOString().slice(0, 16),
      status: 1,
      comment: ''
    };
    this.showAddMedicationModal.set(true);
  }

  closeAddMedicationModal(): void {
    this.showAddMedicationModal.set(false);
  }

  openAddRiskFactorModal(): void {
    this.newRiskFactor = {
      riskFactorID: null,
      notes: ''
    };
    this.showAddRiskFactorModal.set(true);
  }

  closeAddRiskFactorModal(): void {
    this.showAddRiskFactorModal.set(false);
  }

  openAddCurrentIllnessModal(): void {
    this.newCurrentIllness = {
      comments: ''
    };
    this.showAddCurrentIllnessModal.set(true);
  }

  closeAddCurrentIllnessModal(): void {
    this.showAddCurrentIllnessModal.set(false);
  }

  openAddCardiacHistoryModal(): void {
    this.newCardiacHistory = {
      cardiacHistoryID: null,
      comments: ''
    };
    this.showAddCardiacHistoryModal.set(true);
  }

  closeAddCardiacHistoryModal(): void {
    this.showAddCardiacHistoryModal.set(false);
  }

  openAddMedicationHistoryModal(): void {
    this.newMedicationHistory = {
      strength: '',
      notes: ''
    };
    this.showAddMedicationHistoryModal.set(true);
  }

  closeAddMedicationHistoryModal(): void {
    this.showAddMedicationHistoryModal.set(false);
  }

  openAddPatientHistoryModal(): void {
    this.newPatientHistory = {
      illnessHistoryNotes: '',
      medicationHistoryNotes: '',
      cardiacHistoryNotes: '',
      riskFactorNotes: '',
      allergyNotes: '',
      familyHistoryNotes: ''
    };
    this.showAddPatientHistoryModal.set(true);
  }

  closeAddPatientHistoryModal(): void {
    this.showAddPatientHistoryModal.set(false);
  }

  // Additional save methods
  saveMedication(): void {
    const patientId = this.patientId();
    const admissionId = this.admissionId();
    if (!patientId) {
      alert('Patient ID is required');
      return;
    }

    this.isSaving.set(true);
    const data = {
      patientID: patientId,
      admission: admissionId,
      productName: this.newMedication.productName,
      quantity: this.newMedication.quantity,
      direction: this.newMedication.direction || null,
      scheduledDate: this.newMedication.scheduledDate ? new Date(this.newMedication.scheduledDate).toISOString() : new Date().toISOString(),
      status: this.newMedication.status,
      comment: this.newMedication.comment || null,
      createdBy: 1,
      medicalUnit: 1
    };

    this.http.post(`${this.apiUrl}/medication`, data).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeAddMedicationModal();
        const currentPatientId = this.patientId();
        const currentAdmissionId = this.admissionId();
        if (currentPatientId) {
          this.loadMedicalFileByPatientId(currentPatientId);
        } else if (currentAdmissionId) {
          this.loadMedicalFileByAdmissionId(currentAdmissionId);
        }
      },
      error: (err) => {
        console.error('Error saving medication:', err);
        alert('Failed to save medication. Please try again.');
        this.isSaving.set(false);
      }
    });
  }

  saveRiskFactor(): void {
    const patientId = this.patientId();
    if (!patientId) {
      alert('Patient ID is required');
      return;
    }

    this.isSaving.set(true);
    const data = {
      patientID: patientId,
      riskFactorID: this.newRiskFactor.riskFactorID,
      notes: this.newRiskFactor.notes || null,
      createdBy: 1
    };

    this.http.post(`${this.apiUrl}/riskfactor`, data).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeAddRiskFactorModal();
        const currentPatientId = this.patientId();
        const currentAdmissionId = this.admissionId();
        if (currentPatientId) {
          this.loadMedicalFileByPatientId(currentPatientId);
        } else if (currentAdmissionId) {
          this.loadMedicalFileByAdmissionId(currentAdmissionId);
        }
      },
      error: (err) => {
        console.error('Error saving risk factor:', err);
        alert('Failed to save risk factor. Please try again.');
        this.isSaving.set(false);
      }
    });
  }

  saveCurrentIllness(): void {
    const patientId = this.patientId();
    const admissionId = this.admissionId();
    if (!patientId) {
      alert('Patient ID is required');
      return;
    }

    this.isSaving.set(true);
    const data = {
      patientID: patientId,
      admissionID: admissionId,
      comments: this.newCurrentIllness.comments,
      commentsHTML: this.newCurrentIllness.comments,
      createdBy: 1
    };

    this.http.post(`${this.apiUrl}/currentillness`, data).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeAddCurrentIllnessModal();
        const currentPatientId = this.patientId();
        const currentAdmissionId = this.admissionId();
        if (currentPatientId) {
          this.loadMedicalFileByPatientId(currentPatientId);
        } else if (currentAdmissionId) {
          this.loadMedicalFileByAdmissionId(currentAdmissionId);
        }
      },
      error: (err) => {
        console.error('Error saving current illness:', err);
        alert('Failed to save current illness. Please try again.');
        this.isSaving.set(false);
      }
    });
  }

  saveCardiacHistory(): void {
    const patientId = this.patientId();
    if (!patientId) {
      alert('Patient ID is required');
      return;
    }

    this.isSaving.set(true);
    const data = {
      patientID: patientId,
      cardiacHistoryID: this.newCardiacHistory.cardiacHistoryID,
      comments: this.newCardiacHistory.comments || null,
      commentsHTML: this.newCardiacHistory.comments || null,
      createdBy: 1
    };

    this.http.post(`${this.apiUrl}/cardiachistory`, data).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeAddCardiacHistoryModal();
        const currentPatientId = this.patientId();
        const currentAdmissionId = this.admissionId();
        if (currentPatientId) {
          this.loadMedicalFileByPatientId(currentPatientId);
        } else if (currentAdmissionId) {
          this.loadMedicalFileByAdmissionId(currentAdmissionId);
        }
      },
      error: (err) => {
        console.error('Error saving cardiac history:', err);
        alert('Failed to save cardiac history. Please try again.');
        this.isSaving.set(false);
      }
    });
  }

  saveMedicationHistory(): void {
    const patientId = this.patientId();
    if (!patientId) {
      alert('Patient ID is required');
      return;
    }

    this.isSaving.set(true);
    const data = {
      patientID: patientId,
      strength: this.newMedicationHistory.strength || null,
      notes: this.newMedicationHistory.notes || null,
      notesHTML: this.newMedicationHistory.notes || null,
      createdBy: 1
    };

    this.http.post(`${this.apiUrl}/medicationhistory`, data).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeAddMedicationHistoryModal();
        const currentPatientId = this.patientId();
        const currentAdmissionId = this.admissionId();
        if (currentPatientId) {
          this.loadMedicalFileByPatientId(currentPatientId);
        } else if (currentAdmissionId) {
          this.loadMedicalFileByAdmissionId(currentAdmissionId);
        }
      },
      error: (err) => {
        console.error('Error saving medication history:', err);
        alert('Failed to save medication history. Please try again.');
        this.isSaving.set(false);
      }
    });
  }

  savePatientHistory(): void {
    const patientId = this.patientId();
    if (!patientId) {
      alert('Patient ID is required');
      return;
    }

    this.isSaving.set(true);
    const data = {
      patientID: patientId,
      illnessHistoryNotes: this.newPatientHistory.illnessHistoryNotes || null,
      medicationHistoryNotes: this.newPatientHistory.medicationHistoryNotes || null,
      cardiacHistoryNotes: this.newPatientHistory.cardiacHistoryNotes || null,
      riskFactorNotes: this.newPatientHistory.riskFactorNotes || null,
      allergyNotes: this.newPatientHistory.allergyNotes || null,
      familyHistoryNotes: this.newPatientHistory.familyHistoryNotes || null,
      createdBy: 1
    };

    this.http.post(`${this.apiUrl}/patienthistory`, data).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeAddPatientHistoryModal();
        const currentPatientId = this.patientId();
        const currentAdmissionId = this.admissionId();
        if (currentPatientId) {
          this.loadMedicalFileByPatientId(currentPatientId);
        } else if (currentAdmissionId) {
          this.loadMedicalFileByAdmissionId(currentAdmissionId);
        }
      },
      error: (err) => {
        console.error('Error saving patient history:', err);
        alert('Failed to save patient history. Please try again.');
        this.isSaving.set(false);
      }
    });
  }
}



